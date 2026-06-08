using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MemoSphere.WPF.Services
{
    public class ClientQuestionService : BaseClientService, IQuestionService
    {
        public ClientQuestionService(HttpClient httpClient) : base(httpClient)
        {
        }

        // GET api/questions/by-topic/{topicId}
        public async Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/questions/by-topic/{topicId}");
            await EnsureSuccessOrThrowAsync(response, "Hiba a témakörhöz tartozó kérdések lekérésekor");

            return await response.Content.ReadFromJsonAsync<IEnumerable<Question>>() ?? new List<Question>();
        }

        // GET api/questions/for-note/{noteId}
        public async Task<IEnumerable<Question>> GetQuestionsForNoteAsync(int noteId)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/questions/for-note/{noteId}");

            await EnsureSuccessOrThrowAsync(response, "Hiba a jegyzethez tartozó kérdések lekérésekor");

            return await response.Content.ReadFromJsonAsync<IEnumerable<Question>>() ?? new List<Question>();
        }

        // POST api/questions/generate-ai?noteId={noteId}&type={type}
        public async Task<bool> GenerateAndSaveQuestionsAsync(int noteId, QuestionType type)
        {
            PrepareHeaders();

            var url = $"api/questions/generate-ai?noteId={noteId}&type={(int)type}";
            var response = await _httpClient.PostAsync(url, null);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return false;
            }
            await EnsureSuccessOrThrowAsync(response, "Hiba az AI kérdésgenerálás során");

            return true;
        }

        private record ShortAnswerEvaluationResponse(bool IsCorrect, string Explanation);

        // POST api/questions/evaluate-short-answer?questionId={questionId}
        public async Task<(bool IsCorrect, string Explanation)> EvaluateUserShortAnswerAsync(int questionId, string userAnswer)
        {
            PrepareHeaders();

            var url = $"api/questions/evaluate-short-answer?questionId={questionId}";
            var response = await _httpClient.PostAsJsonAsync(url, userAnswer);

            await EnsureSuccessOrThrowAsync(response, "Hiba a rövid válasz kiértékelésekor");

            var result = await response.Content.ReadFromJsonAsync<ShortAnswerEvaluationResponse>();
            if (result == null)
            {
                throw new InvalidOperationException("Nem sikerült beolvasni a kiértékelés eredményét a szervertől.");
            }

            return (result.IsCorrect, result.Explanation);
        }

        // POST api/questions/record-answer?questionId={questionId}&isCorrect={isCorrect}
        public async Task RecordAnswerAsync(int questionId, bool isCorrect)
        {
            PrepareHeaders();

            var url = $"api/questions/record-answer?questionId={questionId}&isCorrect={isCorrect.ToString().ToLower()}";
            var response = await _httpClient.PostAsync(url, null);

            await EnsureSuccessOrThrowAsync(response, "Hiba a válaszstatisztika rögzítésekor");
        }

        // GET api/questions/weighted?topicId={topicId}&count={count}&type={type}
        public async Task<List<Question>> GetWeightedQuestionsAsync(int topicId, int count, QuestionType? type = null)
        {
            PrepareHeaders();

            var url = $"api/questions/weighted?topicId={topicId}&count={count}";
            if (type.HasValue)
            {
                url += $"&type={(int)type.Value}";
            }

            var response = await _httpClient.GetAsync(url);

            await EnsureSuccessOrThrowAsync(response, "Hiba a súlyozott kérdések lekérésekor");

            var questions = await response.Content.ReadFromJsonAsync<List<Question>>();
            return questions ?? new List<Question>();
        }

        // POST api/questions/save-batch
        public async Task SaveQuestionsAsync(IEnumerable<Question> questions)
        {
            PrepareHeaders();

            var questionList = questions.ToList();
            var response = await _httpClient.PostAsJsonAsync("api/questions/save-batch", questionList);

            await EnsureSuccessOrThrowAsync(response, "Hiba a kérdések batch mentése során");
        }

        // DELETE api/questions/{id}
        public async Task DeleteQuestionAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.DeleteAsync($"api/questions/{id}");

            await EnsureSuccessOrThrowAsync(response, $"Hiba a kérdés törlésekor (id: {id})");
        }

        // DELETE api/questions/for-note/{noteId}
        public async Task DeleteQuestionsForNoteAsync(int noteId)
        {
            PrepareHeaders();

            var response = await _httpClient.DeleteAsync($"api/questions/for-note/{noteId}");

            await EnsureSuccessOrThrowAsync(response, $"Hiba a jegyzethez tartozó kérdések törlésekor (noteId: {noteId})");
        }
    }
}