using MemoSphere.Core.Services;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using Core.Interfaces;
using Core.Models;

public class GeminiService : IQuestionGeneratorService
{
    private readonly string _apiKey;
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
    private const string DefaultGeminiModelName = "gemini-2.5-flash";

    public GeminiService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, string modelNameOverride = null)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.Timeout = TimeSpan.FromMinutes(1);
            string actualGeminiModel = string.IsNullOrEmpty(modelNameOverride) ? DefaultGeminiModelName : modelNameOverride;

            string prompt = $@"A következő magyar nyelvű szöveg alapján generálj **pontosan 3 (három) darab**, számozott kérdés-válasz párt.

**Minden egyes kérdés-válasz pár a következő formátumot kövesse, szigorúan ezen sorrendben:**
[Kérdés sorszáma]. [A kérdés szövege]?
Válasz: [A helyes válasz szövege]

**Fontos utasítások:**
- A kérdések legyenek tényalapúak és informatívak.
- A válaszok legyenek egyértelműek, tömörek és kizárólag a megadott szövegből származó információkat tartalmazzák.
- Ne tegyél fel olyan kérdést, amire a szöveg nem ad egyértelmű választ.
- Kerüld az ismétlődő, túl hasonló vagy igen/nem válaszos kérdéseket.
- A magyar nyelvtan és helyesírás legyen hibátlan, természetes hangzású.
- Ne adj hozzá semmilyen magyarázatot, kommentárt, bevezető vagy záró szöveget a kért kérdés-válasz listán kívül. Csak a 3 kérdés-válasz pár listáját add vissza!

Szöveg:
{context}

Kérdések és Válaszok:
";

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

                if (!string.IsNullOrEmpty(generatedText))
                {
                    var qaPairs = new List<QuestionAnswerPair>();
                    string[] lines = generatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                    QuestionAnswerPair currentPair = null;
                    int currentQuestionNumber = 1;

                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                        Match questionMatch = Regex.Match(trimmedLine, @"^\s*(\d+)\.\s*(.+?\?)\s*$", RegexOptions.IgnoreCase);
                        Match answerMatch = Regex.Match(trimmedLine, @"^Válasz:\s*(.+)$", RegexOptions.IgnoreCase);

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
                return new List<QuestionAnswerPair>();
            }
            catch (HttpRequestException)
            {
                throw new Exception("Hiba történt a Google Gemini API-val való kommunikáció során. Ellenőrizze a hálózatot és az API kulcsot.");
            }
            catch (JsonException)
            {
                throw new Exception("Érvénytelen JSON formátumú válasz érkezett a Gemini API-tól.");
            }
            catch (Exception e)
            {
                throw new Exception($"Váratlan hiba történt a Gemini kérdésgenerálás során: {e.Message}");
            }
        }
    }
    public async Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelNameOverride = null)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.Timeout = TimeSpan.FromMinutes(1);
            string actualGeminiModel = string.IsNullOrEmpty(modelNameOverride) ? DefaultGeminiModelName : modelNameOverride;

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

                if (!string.IsNullOrEmpty(generatedText))
                {
                    var wrongAnswers = new List<string>();
                    string[] lines = generatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        wrongAnswers.Add(line.Trim());
                    }
                    return wrongAnswers;
                }
                return new List<string>();
            }
            catch (HttpRequestException)
            {
                throw new Exception("Hiba történt a Google Gemini API-val való kommunikáció során.");
            }
            catch (JsonException)
            {
                throw new Exception("Érvénytelen JSON formátumú válasz érkezett a Gemini API-tól.");
            }
            catch (Exception e)
            {
                throw new Exception($"Váratlan hiba történt a rossz válasz generálás során: {e.Message}");
            }
        }
    }
}