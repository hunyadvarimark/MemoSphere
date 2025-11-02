using Core.Models;
using Core.Enums;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using Core.Interfaces.Services;

public class OllamaService : IQuestionGeneratorService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string OllamaApiUrl = "http://localhost:11434/api/generate";

    static OllamaService()
    {
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    private class OllamaApiResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }
    }

    // =======================================================
    // KÖZÖS API HÍVÁS METÓDUS
    // =======================================================
    private async Task<string> CallOllamaApiAsync(string prompt, string modelName)
    {
        var payload = new
        {
            model = modelName,
            prompt = prompt,
            stream = false,
            options = new { temperature = 0.6, top_p = 0.8, num_predict = 1000 }
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(OllamaApiUrl, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            OllamaApiResponse apiResponse = JsonConvert.DeserializeObject<OllamaApiResponse>(responseBody);

            return apiResponse?.Response ?? string.Empty;
        }
        catch (HttpRequestException)
        {
            throw new Exception("Hiba történt az Ollama szerverrel való kommunikáció során. Ellenőrizd, hogy fut-e az Ollama.");
        }
        catch (JsonException)
        {
            throw new Exception("Érvénytelen JSON formátumú válasz érkezett az Ollama API-tól.");
        }
        catch (Exception e)
        {
            throw new Exception($"Váratlan hiba történt az Ollama API hívás során: {e.Message}");
        }
    }

    // =======================================================
    // KÉRDÉS GENERÁLÁS
    // =======================================================
    public async Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, QuestionType type, string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            throw new ArgumentException("Az Ollama számára meg kell adni a modell nevét.", nameof(modelName));
        }

        string prompt;
        (string questionRegex, string answerRegex) parsingRules;

        switch (type)
        {
            case QuestionType.MultipleChoice:
                prompt = GetMultipleChoicePrompt(context);
                parsingRules = GetMultipleChoiceParsingRules();
                break;

            case QuestionType.TrueFalse:
                prompt = GetTrueFalsePrompt(context);
                parsingRules = GetTrueFalseParsingRules();
                break;

            case QuestionType.ShortAnswer:
                prompt = GetShortAnswerPrompt(context);
                parsingRules = GetShortAnswerParsingRules();
                break;

            default:
                throw new ArgumentException($"Ismeretlen kérdéstípus az Ollama számára: {type}");
        }

        string generatedText = await CallOllamaApiAsync(prompt, modelName);

        if (!string.IsNullOrEmpty(generatedText))
        {
            return ParseResponse(generatedText, parsingRules.questionRegex, parsingRules.answerRegex);
        }
        return new List<QuestionAnswerPair>();
    }

    // =======================================================
    // HIBÁS VÁLASZOK GENERÁLÁSA
    // =======================================================
    public async Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            throw new ArgumentException("Az Ollama számára meg kell adni a modell nevét.", nameof(modelName));
        }

        string prompt = $@"A következő szövegkörnyezetből a helyes válasz: '{correctAnswer}'.
Generálj **pontosan 3 (három) darab**, hihető, de hibás alternatívát a feleletválasztós kérdéshez.
            
**Fontos utasítások:**
- A válaszok legyenek a szövegkörnyezetből származó tények, de ne a helyes válasz.
- A válaszok legyenek hihetőek, ne nyilvánvalóan hibásak.
- Ne adj hozzá semmilyen magyarázatot, kommentárt vagy bevezető szöveget.
- Csak a sorszámozott listát add vissza!
            
Szövegkörnyezet:
{context}
            
Hibás válaszok:
";

        string generatedText = await CallOllamaApiAsync(prompt, modelName);

        if (!string.IsNullOrEmpty(generatedText))
        {
            var wrongAnswers = new List<string>();
            string[] lines = generatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string cleanLine = Regex.Replace(line.Trim(), @"^\d+\.\s*", ""); // Sorszám eltávolítása
                if (!string.IsNullOrWhiteSpace(cleanLine))
                {
                    wrongAnswers.Add(cleanLine);
                }
            }
            return wrongAnswers;
        }
        return new List<string>();
    }

    // =======================================================
    // RÖVID VÁLASZ KIÉRTÉKELÉSE
    // =======================================================
    public async Task<bool> EvaluateAnswerAsync(string questionText, string userAnswer, string correctAnswer, string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            throw new ArgumentException("Az Ollama számára meg kell adni a modell nevét.", nameof(modelName));
        }

        string prompt = $@"

Kérdés: {questionText}

Felhasználó válasza: {userAnswer}

Helyes példa válasz: {correctAnswer}

Értékeld, hogy a felhasználói válasz helyes-e. Szinonima variációk és eltérő megfogalmazások elfogadhatóak, ha a lényegi tartalma megegyezik a helyes válasszal.

**Válaszolj CSAK 'true' vagy 'false' szóval, semmi mással!**";

        string generatedText = await CallOllamaApiAsync(prompt, modelName);

        string cleanResponse = generatedText.Trim().ToLower();
        return cleanResponse == "true" || cleanResponse == "igaz";
    }

    // =======================================================
    // PARSING LOGIKA
    // =======================================================
    private List<QuestionAnswerPair> ParseResponse(string generatedText, string questionRegex, string answerRegex)
    {
        var qaPairs = new List<QuestionAnswerPair>();
        string[] lines = generatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        QuestionAnswerPair currentPair = null;
        int currentQuestionNumber = 1;

        Regex qRegex = new Regex(questionRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Regex aRegex = new Regex(answerRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

            Match questionMatch = qRegex.Match(trimmedLine);
            Match answerMatch = aRegex.Match(trimmedLine);

            if (questionMatch.Success)
            {
                int parsedQuestionNumber = int.Parse(questionMatch.Groups[1].Value);
                if (parsedQuestionNumber == currentQuestionNumber && qaPairs.Count < 3)
                {
                    currentPair = new QuestionAnswerPair { Question = questionMatch.Groups[2].Value.Trim() };
                }
                else
                {
                    currentPair = null;
                }
            }
            else if (answerMatch.Success)
            {
                if (currentPair != null && string.IsNullOrEmpty(currentPair.Answer))
                {
                    currentPair.Answer = answerMatch.Groups[1].Value.Trim();
                    qaPairs.Add(currentPair);
                    currentPair = null;
                    currentQuestionNumber++;
                }
            }
        }
        return qaPairs;
    }

    // =======================================================
    // PROMPT GENERÁTOROK
    // =======================================================
    private string GetMultipleChoicePrompt(string context)
    {
        return $@"A következő magyar nyelvű szöveg alapján generálj **pontosan 3 (három) darab**, számozott feleletválasztós típusú kérdés-válasz párt.

**Minden egyes kérdés-válasz pár a következő formátumot kövesse, szigorúan ezen sorrendben:**
[Kérdés sorszáma]. [A kérdés szövege]?
Válasz: [A helyes válasz szövege]

**Fontos utasítások:**
- A válasz feleletválasztós típushoz legyen alkalmas.
- Ne adj hozzá semmilyen magyarázatot, kommentárt, bevezető vagy záró szöveget. Csak a 3 kérdés-válasz pár listáját add vissza!

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
- A kérdések legyenek tényalapúak, és igényljenek rövid, tömör magyarázatot.
- Kerüld az igen/nem válasszal megválaszolható kérdéseket.
- A válasz maximum 1-2 mondat legyen.
- Ne adj hozzá semmilyen magyarázatot, kommentárt, bevezető vagy záró szöveget. Csak a 3 kérdés-válasz pár listáját add vissza!

Szöveg:
{context}

Kérdések és Válaszok:
";
    }

    // =======================================================
    // PARSING SZABÁLYOK
    // =======================================================
    private (string questionRegex, string answerRegex) GetMultipleChoiceParsingRules()
    {
        return (@"^\s*(\d+)\.\s*(.+?\?)\s*$", @"^Válasz:\s*(.+)$");
    }

    private (string questionRegex, string answerRegex) GetShortAnswerParsingRules()
    {
        return GetMultipleChoiceParsingRules();
    }

    private (string questionRegex, string answerRegex) GetTrueFalseParsingRules()
    {
        return (@"^\s*(\d+)\.\s*(.+)\s*$", @"^Válasz:\s*(IGAZ|HAMIS)$");
    }

    public Task<string> CleanupAndFormatNoteAsync(string rawText, string modelNameOverride = null)
    {
        throw new NotImplementedException();
    }
}