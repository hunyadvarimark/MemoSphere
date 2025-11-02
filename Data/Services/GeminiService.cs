using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models;

public class GeminiService : IQuestionGeneratorService
{
    private readonly string _apiKey;
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
    private const string DefaultGeminiModelName = "gemini-2.5-flash";

    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    public GeminiService(string apiKey)
    {
        _apiKey = apiKey;
    }

    private async Task<string> CallGeminiApiAsync(string prompt, string modelNameOverride = null, bool isCleanupTask = false)
    {
        string actualGeminiModel = string.IsNullOrEmpty(modelNameOverride) ? DefaultGeminiModelName : modelNameOverride;
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = isCleanupTask ? 0.1 : 0.6,
                topP = 0.7,
                topK = isCleanupTask ? 20 : 40,
                maxOutputTokens = 16384  // Maxon, de kisebb chunk-kal kerüzzük a hibát
            },
            safetySettings = isCleanupTask ? new[]
            {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
            } : null
        };
        string jsonRequest = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        string requestUrl = $"{GeminiApiBaseUrl}{actualGeminiModel}:generateContent?key={_apiKey}";
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠️ Gemini HTTP hiba: {response.StatusCode}");
                Console.WriteLine($"⚠️ Válasz: {responseBody}");
            }
            response.EnsureSuccessStatusCode();
            dynamic geminiResponse = JsonConvert.DeserializeObject(responseBody);
            if (geminiResponse?.candidates?[0]?.finishReason != null)
            {
                string finishReason = geminiResponse.candidates[0].finishReason.ToString();
                Console.WriteLine($"🔍 Finish reason: {finishReason}");
                if (finishReason == "MAX_TOKENS")
                {
                    Console.WriteLine("⚠️ MAX_TOKENS - válasz levágva, de visszaadjuk amit kaptunk");
                    string partialText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text;
                    return partialText ?? string.Empty;
                }
                if (finishReason == "SAFETY")
                {
                    Console.WriteLine("⚠️ SAFETY filter aktiválva");
                    return string.Empty;
                }
            }
            string generatedText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text;
            return generatedText ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ HTTP hiba: {ex.Message}");
            throw new Exception("Hiba történt a Gemini API-val való kommunikáció során.");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ JSON hiba: {ex.Message}");
            throw new Exception("Érvénytelen JSON formátumú válasz érkezett a Gemini API-tól.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Váratlan hiba: {e.Message}");
            throw new Exception($"Váratlan hiba történt a Gemini API hívás során: {e.Message}");
        }
    }

    public async Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, QuestionType type, string modelNameOverride = null)
    {
        string prompt;
        Func<string, List<QuestionAnswerPair>> parser;
        switch (type)
        {
            case QuestionType.MultipleChoice:
                prompt = GetMultipleChoicePrompt(context);
                parser = ParseMultipleChoiceResponse;
                break;
            case QuestionType.TrueFalse:
                prompt = GetTrueFalsePrompt(context);
                parser = ParseTrueFalseResponse;
                break;
            case QuestionType.ShortAnswer:
                prompt = GetShortAnswerPrompt(context);
                parser = ParseShortAnswerResponse;
                break;
            default:
                throw new ArgumentException($"Ismeretlen kérdéstípus: {type}");
        }
        string generatedText = await CallGeminiApiAsync(prompt, modelNameOverride);
        if (!string.IsNullOrEmpty(generatedText))
        {
            return parser(generatedText);
        }
        return new List<QuestionAnswerPair>();
    }

    public async Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelNameOverride = null)
    {
        return new List<string>();
    }

    public async Task<bool> EvaluateAnswerAsync(string questionText, string userAnswer, string correctAnswer, string modelNameOverride = null)
    {
        string prompt = $@"
Kérdés: {questionText}
Helyes példa válasz: {correctAnswer}
Felhasználói válasz: {userAnswer}
Értékeld a felhasználói választ az alábbi kritériumok alapján:
- True, ha a lényegi tények 80%-ban egyeznek (szinonimák, eltérő megfogalmazás OK).
- True, ha kulcsszavak (pl. nevek, dátumok) helyesek, még ha rövidebb/hosszabb a válasz.
- False, ha kulcstény hibás vagy hiányzik.(kulcstény ami a konkrét kérdés megválaszolásához szükséges)
Példák:
- Helyes példa: ""Athén vezető lett a görög világban."" User: ""Athén hegemóniát szerzett."" -> true (szinonima).
- Helyes példa: ""Periklész aranykora."" User: ""Demokrácia virágzott Athénban."" -> true (lényeg egyezik).
- Helyes példa: ""Déloszi Szövetség."" User: ""Athén szövetséget kötött."" -> true (implicit).
- Helyes példa: ""Athén erősödött."" User: ""Spárta győzött."" -> false (téves tény).
Válaszolj csak 'true' vagy 'false' értékkel, magyarázat nélkül.";
        string generatedText = await CallGeminiApiAsync(prompt, modelNameOverride);
        string cleanResponse = generatedText.Trim().ToLower();
        return cleanResponse == "true" || cleanResponse == "igaz";
    }

    private List<QuestionAnswerPair> ParseMultipleChoiceResponse(string generatedText)
    {
        var qaPairs = new List<QuestionAnswerPair>();
        string[] lines = generatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        Regex questionRegex = new Regex(@"^\s*(\d+)\.\s*(.+?\?)\s*$", RegexOptions.IgnoreCase);
        Regex correctAnswerRegex = new Regex(@"^Helyes\s+válasz:\s*(.+)$", RegexOptions.IgnoreCase);
        Regex wrongAnswerRegex = new Regex(@"^[A-D]\)\s*(.+)$", RegexOptions.IgnoreCase);
        QuestionAnswerPair currentPair = null;
        bool readingWrongAnswers = false;
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
            Match questionMatch = questionRegex.Match(trimmedLine);
            Match correctMatch = correctAnswerRegex.Match(trimmedLine);
            Match wrongMatch = wrongAnswerRegex.Match(trimmedLine);
            if (questionMatch.Success)
            {
                if (currentPair != null && !string.IsNullOrEmpty(currentPair.Question) && !string.IsNullOrEmpty(currentPair.Answer))
                {
                    qaPairs.Add(currentPair);
                }
                currentPair = new QuestionAnswerPair
                {
                    Question = questionMatch.Groups[2].Value.Trim(),
                    WrongAnswers = new List<string>()
                };
                readingWrongAnswers = false;
            }
            else if (correctMatch.Success && currentPair != null)
            {
                currentPair.Answer = correctMatch.Groups[1].Value.Trim();
                readingWrongAnswers = true;
            }
            else if (wrongMatch.Success && currentPair != null && readingWrongAnswers)
            {
                currentPair.WrongAnswers.Add(wrongMatch.Groups[1].Value.Trim());
            }
        }
        if (currentPair != null && !string.IsNullOrEmpty(currentPair.Question) && !string.IsNullOrEmpty(currentPair.Answer))
        {
            qaPairs.Add(currentPair);
        }
        return qaPairs.Where(q => q.WrongAnswers != null && q.WrongAnswers.Count >= 2).Take(3).ToList();
    }

    private List<QuestionAnswerPair> ParseTrueFalseResponse(string generatedText)
    {
        var qaPairs = new List<QuestionAnswerPair>();
        string[] lines = generatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        Regex questionRegex = new Regex(@"^\s*(\d+)\.\s*(.+)\s*$", RegexOptions.IgnoreCase);
        Regex answerRegex = new Regex(@"^Válasz:\s*(IGAZ|HAMIS)$", RegexOptions.IgnoreCase);
        QuestionAnswerPair currentPair = null;
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
            Match questionMatch = questionRegex.Match(trimmedLine);
            Match answerMatch = answerRegex.Match(trimmedLine);
            if (questionMatch.Success)
            {
                if (qaPairs.Count < 3)
                {
                    currentPair = new QuestionAnswerPair
                    {
                        Question = questionMatch.Groups[2].Value.Trim(),
                        WrongAnswers = new List<string>()
                    };
                }
                else
                {
                    currentPair = null;
                }
            }
            else if (answerMatch.Success && currentPair != null)
            {
                string answer = answerMatch.Groups[1].Value.Trim().ToUpper();
                currentPair.Answer = answer;
                currentPair.WrongAnswers.Add(answer == "IGAZ" ? "HAMIS" : "IGAZ");
                qaPairs.Add(currentPair);
                currentPair = null;
            }
        }
        return qaPairs;
    }

    private List<QuestionAnswerPair> ParseShortAnswerResponse(string generatedText)
    {
        var qaPairs = new List<QuestionAnswerPair>();
        string[] lines = generatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        Regex questionRegex = new Regex(@"^\s*(\d+)\.\s*(.+?\?)\s*$", RegexOptions.IgnoreCase);
        Regex answerRegex = new Regex(@"^Válasz:\s*(.+)$", RegexOptions.IgnoreCase);
        QuestionAnswerPair currentPair = null;
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
            Match questionMatch = questionRegex.Match(trimmedLine);
            Match answerMatch = answerRegex.Match(trimmedLine);
            if (questionMatch.Success)
            {
                if (qaPairs.Count < 3)
                {
                    currentPair = new QuestionAnswerPair { Question = questionMatch.Groups[2].Value.Trim() };
                }
                else
                {
                    currentPair = null;
                }
            }
            else if (answerMatch.Success && currentPair != null && string.IsNullOrEmpty(currentPair.Answer))
            {
                currentPair.Answer = answerMatch.Groups[1].Value.Trim();
                qaPairs.Add(currentPair);
                currentPair = null;
            }
        }
        return qaPairs;
    }

    private string GetMultipleChoicePrompt(string context)
    {
        return $@"A következő magyar nyelvű szöveg alapján generálj **pontosan 3 (három) darab**, számozott feleletválasztós kérdés-válasz párt.
**Minden egyes kérdés-válasz pár a következő formátumot kövesse, szigorúan ezen sorrendben:**
[Kérdés sorszáma]. [A kérdés szövege]?
Helyes válasz: [A helyes válasz szövege]
A) [Első rossz válasz]
B) [Második rossz válasz]
C) [Harmadik rossz válasz]
**Fontos utasítások:**
- A kérdések legyenek tényalapúak és informatívak.
- A helyes válasz legyen egyértelmű, tömör és kizárólag a megadott szövegből származó információt tartalmazzon.
- A 3 rossz válasz legyen hihető, a szövegkörnyezetből származó, de helytelen információ.
- A rossz válaszok ne legyenek nyilvánvalóan hibásak.
- Ne adj hozzá semmilyen magyarázatot, kommentárt, bevezető vagy záró szöveget. Csak a 3 kérdés-válasz pár listáját add vissza!
- Példa feladatokra ne kérdezz rá, ha vannak a szövegben.
Szöveg:
{context}
Kérdések és Válaszok:
";
    }

    private string GetTrueFalsePrompt(string context)
    {
        return $@"A következő magyar nyelvű szöveg alapján generálj **pontosan 3 (három) darab**, számozott eldöntendő (Igaz/Hamis) kérdés-válasz párt.
**Minden egyes kérdés-válasz pár a következő formátumot kövesse, szigorúan ezen sorrendben:**
[Kérdés sorszáma]. [Egy tényállítás (NE használj kérdőjelet)]
Válasz: [IGAZ vagy HAMIS]
**Fontos utasítások:**
- A válaszok kizárólag 'IGAZ' vagy 'HAMIS' szövegek lehetnek.
- A kérdések fele (vagy ehhez közeli arányban) legyen hamis állítás (Válasz: HAMIS).
- Ne adj hozzá semmilyen magyarázatot, kommentárt, bevezető vagy záró szöveget. Csak a 3 kérdés-válasz pár listáját add vissza!
- Példa feladatokra ne kérdezz rá, ha vannak a szövegben.
Szöveg:
{context}
Kérdések és Válaszok:
";
    }

    private string GetShortAnswerPrompt(string context)
    {
        return $@"A következő magyar nyelvű szöveg alapján generálj **pontosan 3 (három) darab**, számozott kifejtős (rövid válasz) kérdés-válasz párt.
**Minden egyes kérdés-válasz pár a következő formátumot kövesse, szigorúan ezen sorrendben:**
[Kérdés sorszáma]. [A kifejtést igénylő kérdés szövege]?
Válasz: [A rövid, de teljeskörű helyes válasz, ami a megadott szövegből származik]
**Fontos utasítások:**
- A kérdések legyenek tényalapúak, és igényljenek rövid, tömör magyarázatot (pl. Miért? Hogyan?).
- Kerüld az igen/nem válasszal megválaszolható kérdéseket.
- A válasz maximum 1-2 mondat legyen.
- Ne adj hozzá semmilyen magyarázatot, kommentárt, bevezető vagy záró szöveget. Csak a 3 kérdés-válasz pár listáját add vissza!
- Példa feladatokra ne kérdezz rá, ha vannak a szövegben.
Szöveg:
{context}
Kérdések és Válaszok:
";
    }

    public async Task<string> CleanupAndFormatNoteAsync(string promptOrText, string modelNameOverride = null)
    {
        string prompt;
        bool isAlreadyPrompt = promptOrText.Length > 500 &&
                              (promptOrText.Contains("Formázd") ||
                               promptOrText.Contains("Markdown") ||
                               promptOrText.Contains("Ne írj semmit"));
        if (isAlreadyPrompt)
        {
            prompt = promptOrText;
        }
        else
        {
            prompt = $@"Formázd ezt Markdownra:
- Címsorok elé # ## ###
- Ha van matematika: LaTeX
- Töröld: oldalszámokat, fejléceket
- Javítsd a szóközöket
Ne írj semmit, csak a formázott szöveget!
{promptOrText}";
        }
        try
        {
            string result = await CallGeminiApiAsync(prompt, modelNameOverride, isCleanupTask: true);
            if (string.IsNullOrWhiteSpace(result) || result.Length < 100)
            {
                Console.WriteLine("⚠️ AI nem válaszolt megfelelően");
                return string.Empty;
            }
            return PostProcessMarkdown(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Hiba: {ex.Message}");
            return string.Empty;
        }
    }

    private string PostProcessMarkdown(string text)
    {
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @" *\n *", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = text.Replace("---", "");
        text = Regex.Replace(text, @"\$\$([^\$]+)\$\$", m => "$$" + m.Groups[1].Value.Trim() + "$$");
        return text.Trim();
    }
}