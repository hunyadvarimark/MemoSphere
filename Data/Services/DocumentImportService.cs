using Core.Interfaces.Services;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace MemoSphere.Data.Services
{
    public class DocumentImportService : IDocumentImportService
    {
        private readonly IQuestionGeneratorService _questionGeneratorService;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(4);

        public DocumentImportService(IQuestionGeneratorService questionGeneratorService)
        {
            _questionGeneratorService = questionGeneratorService;
        }

        public bool IsPdfFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            return extension == ".pdf";
        }

        public async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("A PDF fájl nem található.", filePath);
            if (!IsPdfFile(filePath))
                throw new ArgumentException("A fájl nem PDF formátumú.", nameof(filePath));
            try
            {
                using (PdfDocument document = PdfDocument.Open(filePath))
                {
                    var text = new StringBuilder();
                    int pageCount = document.NumberOfPages;
                    Console.WriteLine($"📄 PDF betöltve: {pageCount} oldal");
                    for (int i = 1; i <= pageCount; i++)
                    {
                        var page = document.GetPage(i);
                        string pageText = page.Text;
                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            pageText = Regex.Replace(pageText, @"\f+", "\n");
                            pageText = Regex.Replace(pageText, @"[\x00-\x08\x0B\x0E-\x1F]", "");
                            text.AppendLine(pageText);
                            text.AppendLine();
                        }
                        Console.WriteLine($"✓ Oldal {i}/{pageCount} kinyerve");
                    }
                    var rawText = text.ToString().Trim();
                    Console.WriteLine($"📊 Nyers szöveg: {rawText.Length:N0} karakter");
                    var sw = Stopwatch.StartNew();
                    var cleanedText = await ProcessTextInChunksAsync(rawText);
                    Console.WriteLine($"✅ Formázás kész: {cleanedText.Length:N0} karakter (Időtartam: {sw.Elapsed})");
                    return cleanedText;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hiba a PDF feldolgozása során: {ex.Message}");
                throw new InvalidOperationException($"Nem sikerült a PDF feldolgozása: {ex.Message}", ex);
            }
        }

        private async Task<string> ProcessTextInChunksAsync(string rawText)
        {
            const int OptimalChunkSize = 3000;  // Kisebb a token hiba ellen
            if (rawText.Length <= OptimalChunkSize)
            {
                Console.WriteLine("⚡ Rövid szöveg, egy lépésben...");
                return await ProcessSingleChunkAsync(rawText) ?? FallbackFormat(rawText);
            }
            var chunks = SplitIntoChunks(rawText, OptimalChunkSize);
            Console.WriteLine($"📦 Szöveg {chunks.Count} chunk-ra bontva");
            var cleanedChunks = new string[chunks.Count];
            for (int batchStart = 0; batchStart < chunks.Count; batchStart += 4)
            {
                int batchEnd = Math.Min(batchStart + 4, chunks.Count);
                Console.WriteLine($"🔄 Batch {(batchStart / 4) + 1}: chunk {batchStart + 1}-{batchEnd}/{chunks.Count}");
                var batchTasks = new List<Task<(int index, string result)>>();
                for (int i = batchStart; i < batchEnd; i++)
                {
                    int chunkIndex = i;
                    var task = ProcessChunkWithIndexAsync(chunkIndex, chunks[chunkIndex]);
                    batchTasks.Add(task);
                }
                var batchResults = await Task.WhenAll(batchTasks);
                foreach (var (index, result) in batchResults)
                {
                    cleanedChunks[index] = result;
                    Console.WriteLine($" ✓ Chunk {index + 1} kész ({result.Length} kar)");
                }
                if (batchEnd < chunks.Count)
                {
                    await Task.Delay(50); // Csökkentve
                }
            }
            return string.Join("\n\n", cleanedChunks.Where(c => !string.IsNullOrWhiteSpace(c)));
        }

        private async Task<(int index, string result)> ProcessChunkWithIndexAsync(int index, string chunk)
        {
            await _semaphore.WaitAsync();
            try
            {
                var cleaned = await ProcessSingleChunkAsync(chunk);
                if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < Math.Min(200, chunk.Length / 5))
                {
                    Console.WriteLine($"⚠️ Chunk {index + 1} AI nem működött, fallback...");
                    return (index, FallbackFormat(chunk));
                }
                return (index, cleaned);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Chunk {index + 1} hiba: {ex.Message}, fallback...");
                return (index, FallbackFormat(chunk));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string> ProcessSingleChunkAsync(string text)
        {
            if (!ContainsComplexContent(text))
            {
                Console.WriteLine("🔧 Gyors fallback chunk-ra...");
                return FallbackFormat(text);
            }

            string prompt = $@"Formázd ezt a szöveget Markdownra:
- Címsorok elé ###
- Matematika: $$...$$ vagy $...$
- Töröld az oldalszámokat és fejléceket
- Javítsd a szóközöket ahol összeragadt
Ne írj semmit mást, csak a formázott szöveget!
{text}";
            try
            {
                var result = await _questionGeneratorService.CleanupAndFormatNoteAsync(prompt);

                // Ha az AI bármit visszaad, ami nem üres, azt fogadjuk el.
                // Az IsMeaningfullyDifferent ellenőrzés felesleges és káros.
                if (!string.IsNullOrWhiteSpace(result))
                {
                    return PostProcess(result); // Bízzunk az AI eredményében
                }

                // Ha az AI tényleg üres stringet adott, csak akkor van baj
                Console.WriteLine("⚠️ AI üres válasszal tért vissza, fallback...");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AI hívás hiba: {ex.Message}, fallback...");
                return null; // Hiba esetén is fallback
            }
        }

        private bool ContainsComplexContent(string text)
        {
            return Regex.IsMatch(text, @"\$|\\\[|\\\(|[a-záűőúöüóéí][A-ZÁŰŐÚÖÜÓÉÍ]", RegexOptions.Compiled);
        }

        private bool IsMeaningfullyDifferent(string original, string formatted)
        {
            bool hasMarkdown = formatted.Contains("##") ||
                              formatted.Contains("**") ||
                              formatted.Contains("$$") ||
                              (formatted.Contains("$") && formatted.Split('$').Length > 2);
            if (hasMarkdown)
                return true;
            double lengthRatio = (double)formatted.Length / original.Length;
            bool expandedWithFormatting = lengthRatio > 1.1 && lengthRatio < 1.5;
            int originalSpaces = original.Count(c => c == ' ');
            int formattedSpaces = formatted.Count(c => c == ' ');
            bool fixedSpaces = formattedSpaces > originalSpaces * 1.2;
            return hasMarkdown || expandedWithFormatting || fixedSpaces;
        }

        private string PostProcess(string text)
        {
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @" *\n *", "\n");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            text = Regex.Replace(text, @"\$\$\s*([^\$]+?)\s*\$\$", m => "$$" + m.Groups[1].Value.Trim() + "$$");
            return text.Trim();
        }

        private string FallbackFormat(string text)
        {
            Console.WriteLine("🔧 Regex alapú fallback...");
            text = Regex.Replace(text, @"(Oldal|Page)\s*[|\:]\s*\d+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\d{4}\s*[©℗®]?\s*(minden\s*jog|all\s*rights)?.*?fenntartva.*?", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\S+@\S+\.\S+", "");
            text = Regex.Replace(text, @"^(https?://|www\.)\S+", "", RegexOptions.Multiline);
            text = Regex.Replace(text, @"([a-záűőúöüóéíàèìòùâêîôûäëïöüÿ])([A-ZÁŰŐÚÖÜÓÉÍÀÈÌÒÙÂÊÎÔÛÄËÏÖÜŸ][a-z])", "$1 $2");
            text = Regex.Replace(text, @"(\d)([a-záűőúöüóéí])", "$1 $2");
            text = Regex.Replace(text, @"([a-záűőúöüóéí])(\d)", "$1 $2");
            text = Regex.Replace(text, @"^(Tétel|Tètel|Definíció|Definicio|Feladat|Megoldás|Megoldas|Példa|Pelda)", "### $1", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"^(Theorem|Definition|Lemma|Corollary|Proposition|Exercise|Solution|Example|Problem)", "### $1", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"^(\d+\.)+\s+([A-ZÁŰŐÚÖÜÓÉÍ][^\n]{5,60})$", "## $2", RegexOptions.Multiline);
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @" *\n *", "\n");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            return text.Trim();
        }

        private List<string> SplitIntoChunks(string text, int maxChunkSize)
        {
            var chunks = new List<string>();
            var paragraphs = Regex.Split(text, @"\n\n+");
            var currentChunk = new StringBuilder();

            const int OverlapSize = 200;

            foreach (var para in paragraphs)
            {
                var trimmed = para.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                if (currentChunk.Length + trimmed.Length + 2 > maxChunkSize && currentChunk.Length > 0)
                {
                    string fullContent = currentChunk.ToString().Trim();
                    chunks.Add(fullContent);

                    currentChunk.Clear();

                    if (fullContent.Length > OverlapSize)
                    {
                        string rawOverlap = fullContent.Substring(fullContent.Length - OverlapSize);

                        int spaceIndex = rawOverlap.IndexOf(' ');

                        if (spaceIndex > 0 && spaceIndex < rawOverlap.Length - 1)
                        {
                            string cleanOverlap = rawOverlap.Substring(spaceIndex + 1);

                            currentChunk.AppendLine(cleanOverlap);
                            currentChunk.AppendLine();
                        }
                    }
                }

                currentChunk.AppendLine(trimmed);
                currentChunk.AppendLine();
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
            }

            // Fallback: Ha egyetlen bekezdés önmagában nagyobb mint a limit
            if (chunks.Count == 1 && chunks[0].Length > maxChunkSize)
            {
                return SplitBySentences(chunks[0], maxChunkSize);
            }

            return chunks;
        }

        private List<string> SplitBySentences(string text, int maxChunkSize)
        {
            var chunks = new List<string>();
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            var currentChunk = new StringBuilder();
            foreach (var sentence in sentences)
            {
                if (currentChunk.Length + sentence.Length + 1 > maxChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
                currentChunk.Append(sentence).Append(" ");
            }
            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
            }
            return chunks;
        }
    }
}