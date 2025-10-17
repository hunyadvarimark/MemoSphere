using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using Core.Models;
using Core.Enums;
using Core.Interfaces.Services;

public class GeminiService : IQuestionGeneratorService
{
    private readonly string _apiKey;
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
    private const string DefaultGeminiModelName = "gemini-2.5-flash";

    public GeminiService(string apiKey)
    {
        _apiKey = apiKey;
    }

    // =======================================================
    // KÖZÖS API HÍVÁS METÓDUS
    // =======================================================
    private async Task<string> CallGeminiApiAsync(string prompt, string modelNameOverride = null)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.Timeout = TimeSpan.FromMinutes(1);
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
                    temperature = 0.6,
                    topP = 0.8,
                    topK = 40,
                    maxOutputTokens = 4000
                }
            };

            string jsonRequest = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            string requestUrl = $"{GeminiApiBaseUrl}{actualGeminiModel}:generateContent?key={_apiKey}";

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(requestUrl, content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic geminiResponse = JsonConvert.DeserializeObject(responseBody);
                string generatedText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text;

                return generatedText ?? string.Empty;
            }
            catch (HttpRequestException)
            {
                throw new Exception("Hiba történt a Gemini API-val való kommunikáció során.");
            }
            catch (JsonException)
            {
                throw new Exception("Érvénytelen JSON formátumú válasz érkezett a Gemini API-tól.");
            }
            catch (Exception e)
            {
                throw new Exception($"Váratlan hiba történt a Gemini API hívás során: {e.Message}");
            }
        }
    }

    // =======================================================
    // KÉRDÉS GENERÁLÁS
    // =======================================================
    public async Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, QuestionType type, string modelNameOverride = null)
    {
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
                throw new ArgumentException($"Ismeretlen kérdéstípus: {type}");
        }

        string generatedText = await CallGeminiApiAsync(prompt, modelNameOverride);

        if (!string.IsNullOrEmpty(generatedText))
        {
            return ParseResponse(generatedText, parsingRules.questionRegex, parsingRules.answerRegex);
        }
        return new List<QuestionAnswerPair>();
    }

    // =======================================================
    // HIBÁS VÁLASZOK GENERÁLÁSA
    // =======================================================
    public async Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelNameOverride = null)
    {
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

        string generatedText = await CallGeminiApiAsync(prompt, modelNameOverride);

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
    public async Task<bool> EvaluateAnswerAsync(string questionText, string userAnswer, string correctAnswer, string context, string modelNameOverride = null)
    {
        string prompt = $@"Kontextus: {context} (ha használod, különben hagyd ki)
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

    // =======================================================
    // PARSING LOGIKA
    // =======================================================
    private List<QuestionAnswerPair> ParseResponse(string generatedText, string questionRegex, string answerRegex)
    {
        var qaPairs = new List<QuestionAnswerPair>();
        string[] lines = generatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        QuestionAnswerPair currentPair = null;

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
                if (qaPairs.Count < 3)
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
        return $@"A következő magyar nyelvű szöveg alapján generálj **pontosan 3 (három) darab**, számozott kérdés-válasz párt.

**Minden egyes kérdés-válasz pár a következő formátumot kövesse, szigorúan ezen sorrendben:**
[Kérdés sorszáma]. [A kérdés szövege]?
Válasz: [A helyes válasz szövege]

**Fontos utasítások:**
- A kérdések legyenek tényalapúak és informatívak.
- A válaszok legyenek egyértelműek, tömörek és kizárólag a megadott szövegből származó információkat tartalmazzák, és feleletválasztós típushoz legyenek alkalmasak.
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
- A kérdések legyenek tényalapúak, és igényljenek rövid, tömör magyarázatot (pl. Miért? Hogyan?).
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
}