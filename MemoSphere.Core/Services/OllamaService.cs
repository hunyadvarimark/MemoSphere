using Core.Interfaces;
using Core.Interfaces;
using Core.Models;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

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

    public async Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, string modelName)
    {
        // A promptot most már a hívásnál állítjuk össze
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
            string generatedText = apiResponse?.Response;

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
            throw new Exception("Hiba történt az Ollama szerverrel való kommunikáció során. Ellenőrizd, hogy fut-e az Ollama.");
        }
        catch (JsonException)
        {
            throw new Exception("Érvénytelen JSON formátumú válasz érkezett az Ollama API-tól.");
        }
        catch (Exception e)
        {
            throw new Exception($"Váratlan hiba történt az Ollama kérdésgenerálás során: {e.Message}");
        }
    }
    public async Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelName)
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
            string generatedText = apiResponse?.Response;

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
            throw new Exception("Hiba történt az Ollama szerverrel való kommunikáció során.");
        }
        catch (JsonException)
        {
            throw new Exception("Érvénytelen JSON formátumú válasz érkezett az Ollama API-tól.");
        }
        catch (Exception e)
        {
            throw new Exception($"Váratlan hiba történt a rossz válasz generálás során: {e.Message}");
        }
    }
}