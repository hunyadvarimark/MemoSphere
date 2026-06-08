using Core.Entities;
using Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MemoSphere.WPF.Services
{
    public class ClientQuizService : BaseClientService, IQuizService, IActiveLearningService
    {
        public ClientQuizService(HttpClient httpClient) : base(httpClient)
        {
        }

        // ==========================================
        // IQuizService Implementáció
        // ==========================================

        // POST api/quiz/generate?count={count}
        public async Task<List<Question>> GetRandomQuestionsForQuizAsync(List<int> topicIds, int count)
        {
            PrepareHeaders();

            var url = $"api/quiz/generate?count={count}";
            var response = await _httpClient.PostAsJsonAsync(url, topicIds);
            await EnsureSuccessOrThrowAsync(response, "Hiba a kvízkérdések generálásakor");

            return await response.Content.ReadFromJsonAsync<List<Question>>() ?? new List<Question>();
        }

        private record QuestionCountResponse(int TotalQuestions);

        // POST api/quiz/count
        public async Task<int> GetQuestionCountForTopicsAsync(List<int> topicIds)
        {
            PrepareHeaders();

            var response = await _httpClient.PostAsJsonAsync("api/quiz/count", topicIds);

            await EnsureSuccessOrThrowAsync(response, "Hiba a kérdések számának lekérésekor");

            var result = await response.Content.ReadFromJsonAsync<QuestionCountResponse>();
            return result?.TotalQuestions ?? 0;
        }

        // ==========================================
        // IActiveLearningService Implementáció
        // ==========================================

        // GET api/quiz/active-topics
        public async Task<List<ActiveTopic>> GetActiveTopicsAsync()
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync("api/quiz/active-topics");

            await EnsureSuccessOrThrowAsync(response, "Hiba az aktív témakörök listázásakor");

            return await response.Content.ReadFromJsonAsync<List<ActiveTopic>>() ?? new List<ActiveTopic>();
        }
        public async Task<ActiveTopic> GetActiveTopicAsync(int topicId)
        {
            var activeTopics = await GetActiveTopicsAsync();
            return activeTopics.FirstOrDefault(t => t.TopicId == topicId)
                   ?? throw new InvalidOperationException($"A megadott témakör (id: {topicId}) nem aktív vagy nem található.");
        }

        public async Task<bool> IsTopicActiveAsync(int topicId)
        {
            var activeTopics = await GetActiveTopicsAsync();
            return activeTopics.Any(t => t.TopicId == topicId);
        }

        // POST api/quiz/activate?topicId={topicId}&dailyGoal={dailyGoal}
        public async Task ActivateTopicAsync(int topicId, int dailyGoal = 10)
        {
            PrepareHeaders();

            var url = $"api/quiz/activate?topicId={topicId}&dailyGoal={dailyGoal}";
            var response = await _httpClient.PostAsync(url, null);

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakör aktiválásakor");
        }

        // POST api/quiz/deactivate/{topicId}
        public async Task DeactivateTopicAsync(int topicId)
        {
            PrepareHeaders();

            var response = await _httpClient.PostAsync($"api/quiz/deactivate/{topicId}", null);

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakör deaktiválásakor");
        }

        // POST api/quiz/update-progress?topicId={topicId}&isCorrect={isCorrect}
        public async Task UpdateProgressAsync(int topicId, bool isCorrect)
        {
            PrepareHeaders();

            var url = $"api/quiz/update-progress?topicId={topicId}&isCorrect={isCorrect.ToString().ToLower()}";
            var response = await _httpClient.PostAsync(url, null);

            await EnsureSuccessOrThrowAsync(response, "Hiba a haladás frissítésekor");
        }

        // POST api/quiz/check-streaks
        public async Task CheckStreaksOnLoginAsync()
        {
            PrepareHeaders();

            var response = await _httpClient.PostAsync("api/quiz/check-streaks", null);

            await EnsureSuccessOrThrowAsync(response, "Hiba a streak-ek ellenőrzésekor");
        }

        private record TopicStatsResponse(int TopicId, double MasteryPercentage, int TodayQuestionsAnswered);

        // GET api/quiz/topic-stats/{topicId}
        public async Task<double> GetMasteryPercentageAsync(int topicId)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/quiz/topic-stats/{topicId}");

            await EnsureSuccessOrThrowAsync(response, "Hiba a jártassági szint (mastery) lekérésekor");

            var result = await response.Content.ReadFromJsonAsync<TopicStatsResponse>();
            return result?.MasteryPercentage ?? 0.0;
        }

        // GET api/quiz/topic-stats/{topicId}
        public async Task<int> GetTodayQuestionsCountAsync(int topicId)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/quiz/topic-stats/{topicId}");

            await EnsureSuccessOrThrowAsync(response, "Hiba a mai megválaszolt kérdések számának lekérésekor");

            var result = await response.Content.ReadFromJsonAsync<TopicStatsResponse>();
            return result?.TodayQuestionsAnswered ?? 0;
        }
    }
}