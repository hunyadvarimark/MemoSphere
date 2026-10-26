using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

    private async Task<string> CallGeminiApiAsync(string prompt, string modelNameOverride = null, bool isCleanupTask = false, bool isJsonResponse = false)
    {
        string actualGeminiModel = string.IsNullOrEmpty(modelNameOverride) ? DefaultGeminiModelName : modelNameOverride;

        var generationConfig = new Dictionary<string, object>
        {
            { "temperature", isCleanupTask ? 0.1 : 0.6 },
            { "topP", 0.7 },
            { "topK", isCleanupTask ? 20 : 40 },
            { "maxOutputTokens", 8192 }
        };

        if (isJsonResponse)
        {
            generationConfig["responseMimeType"] = "application/json";
        }

        var requestBody = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = prompt } } }
            },
            generationConfig = generationConfig,
            safetySettings = isCleanupTask ? new[]
            {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
            } : null
        };

        string jsonRequest = JsonConvert.SerializeObject(requestBody);
        string requestUrl = $"{GeminiApiBaseUrl}{actualGeminiModel}:generateContent?key={_apiKey}";

        int maxRetries = 3;
        int delayMilliseconds = 2000;

        for (int i = 0; i <= maxRetries; i++)
        {
            try
            {
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, content);

                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (i < maxRetries)
                    {
                        Console.WriteLine($"⚠️ Gemini szerver túlterhelt ({response.StatusCode}). Újrapróbálkozás ({i + 1}/{maxRetries}) {delayMilliseconds}ms múlva...");
                        await Task.Delay(delayMilliseconds);
                        delayMilliseconds *= 2;
                        continue;
                    }
                }

                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic geminiResponse = JsonConvert.DeserializeObject(responseBody);

                if (geminiResponse?.candidates?[0]?.finishReason != null)
                {
                    string finishReason = geminiResponse.candidates[0].finishReason.ToString();
                    if (finishReason == "SAFETY")
                    {
                        Console.WriteLine("⚠️ SAFETY filter aktiválva");
                        return string.Empty;
                    }
                }

                string generatedText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text;
                return generatedText ?? string.Empty;
            }
            catch (Exception ex) when (i < maxRetries)
            {
                Console.WriteLine($"❌ Hiba a hívás során: {ex.Message}. Újrapróbálkozás...");
                await Task.Delay(delayMilliseconds);
                delayMilliseconds *= 2;
            }
        }

        throw new Exception("A Gemini API nem elérhető (túlterhelt). Próbáld újra később.");
    }

    public async Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, QuestionType type, string modelNameOverride = null)
    {
        string prompt = type switch
        {
            QuestionType.MultipleChoice => GetMultipleChoicePrompt(context),
            QuestionType.TrueFalse => GetTrueFalsePrompt(context),
            QuestionType.ShortAnswer => GetShortAnswerPrompt(context),
            _ => throw new ArgumentException($"Ismeretlen kérdéstípus: {type}")
        };

        string generatedText = await CallGeminiApiAsync(prompt, modelNameOverride, isCleanupTask: false, isJsonResponse: true);

        if (string.IsNullOrWhiteSpace(generatedText))
        {
            return new List<QuestionAnswerPair>();
        }

        try
        {
            var pairs = JsonConvert.DeserializeObject<List<QuestionAnswerPair>>(generatedText) ?? new List<QuestionAnswerPair>();

            if (type == QuestionType.MultipleChoice)
            {
                return pairs.Where(q => q.WrongAnswers != null && q.WrongAnswers.Count >= 2).Take(3).ToList();
            }

            return pairs.Take(3).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Hiba a generált JSON deszerializálásakor: {ex.Message}");
            return new List<QuestionAnswerPair>();
        }
    }

    public async Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelNameOverride = null)
    {
        return new List<string>();
    }

    public async Task<(bool IsCorrect, string Explanation)> EvaluateAnswerAsync(string questionText, string userAnswer, string correctAnswer, string context = null, string modelNameOverride = null)
    {
        string contextBlock = string.IsNullOrWhiteSpace(context)
            ? ""
            : $"\nKontextus (Forrásanyag):\n{context}\n----------------\n";

        string prompt = $@"
Kérdés: {questionText}
{contextBlock}
Helyes példa válasz: {correctAnswer}
Felhasználói válasz: {userAnswer}

Értékeld a felhasználói választ KIZÁRÓLAG a megadott kérdés, a fenti kontextus (ha elérhető) és a helyes példa válasz alapján. 
NE HASZNÁLJ KÜLSŐ TUDÁST – csak a szemantikai hasonlóságot vizsgáld!
- True, ha a lényegi tények 80%-ban egyeznek (szinonimák, eltérő megfogalmazás elfogadható).
- True, ha a kulcsszavak (nevek, dátumok, fogalmak) helyesek.
- False, ha egy kulcstény hibás vagy hiányzik.

Válaszolj az alábbi JSON struktúrában:
{{
  ""is_correct"": true,
  ""explanation"": ""Rövid magyar indoklás.""
}}";

        try
        {
            string generatedText = await CallGeminiApiAsync(prompt, modelNameOverride, isCleanupTask: false, isJsonResponse: true);

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                return (false, "Az AI üres választ küldött.");
            }

            var definition = new { is_correct = false, explanation = "" };
            var response = JsonConvert.DeserializeAnonymousType(generatedText, definition);

            if (response == null)
            {
                return (false, "Az AI válasza nem értelmezhető JSON.");
            }

            Console.WriteLine($"🤖 AI Evaluation: {response.is_correct} | Reason: {response.explanation}");
            return (response.is_correct, response.explanation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AI Evaluation Exception: {ex.Message}");
            return (false, "Technikai hiba történt az értékelés során.");
        }
    }

    private string GetMultipleChoicePrompt(string context)
    {
        return $@"A megadott szöveg alapján generálj pontosan 3 darab feleletválasztós kérdés-válasz párt.
A válasz kizárólag egy JSON tömb legyen a következő mezőkkel minden elemnél:
- Question: A kérdés szövege.
- Answer: A helyes válasz.
- WrongAnswers: Egy tömb, amely pontosan 3 darab hihető, de hibás választ tartalmaz.

Példa:
[
  {{
    ""Question"": ""Mikor kezdődött a folyamat?"",
    ""Answer"": ""1945-ben"",
    ""WrongAnswers"": [""1939-ben"", ""1956-ban"", ""1914-ben""]
  }}
]

Szöveg:
{context}";
    }

    private string GetTrueFalsePrompt(string context)
    {
        return $@"A megadott szöveg alapján generálj pontosan 3 darab eldöntendő (Igaz/Hamis) kérdés-válasz párt.
A válasz kizárólag egy JSON tömb legyen a következő mezőkkel minden elemnél:
- Question: Egy egyértelmű állítás (kérdőjel nélkül).
- Answer: ""IGAZ"" vagy ""HAMIS"".
- WrongAnswers: Egyetlen elemet tartalmazó tömb: ha az Answer IGAZ, akkor [""HAMIS""], ha az Answer HAMIS, akkor [""IGAZ""].

Arány: körülbelül fele-fele legyen az igaz és hamis állítások száma.

Szöveg:
{context}";
    }

    private string GetShortAnswerPrompt(string context)
    {
        return $@"A megadott szöveg alapján generálj pontosan 3 darab kifejtős (rövid válaszos) kérdés-válasz párt.
A válasz kizárólag egy JSON tömb legyen a következő mezőkkel minden elemnél:
- Question: A kifejtést igénylő kérdés (Miért? Hogyan?).
- Answer: 1-2 mondatos tömör, helyes mintaválasz a szövegből.
- WrongAnswers: Üres tömb ([]).

Szöveg:
{context}";
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
- Ha van matematika: LaTeX ($...$ vagy $$...$$)
- Töröld: oldalszámokat, fejléceket
- Javítsd a szóközöket

Ne írj semmit, csak a formázott szöveget!
{promptOrText}";
        }

        try
        {
            // Itt isJsonResponse = false, mert nyers Markdown kell
            string result = await CallGeminiApiAsync(prompt, modelNameOverride, isCleanupTask: true, isJsonResponse: false);
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