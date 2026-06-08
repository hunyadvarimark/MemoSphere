using Core.Entities;
using Core.Interfaces.Services;
using System.Net.Http;
using System.Net.Http.Json;

namespace MemoSphere.WPF.Services
{
    public class ClientTopicService : BaseClientService, ITopicService
    {
        public ClientTopicService(HttpClient httpClient) : base(httpClient)
        {
        }

        // GET api/topics
        public async Task<IEnumerable<Topic>> GetAllTopicsAsync()
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync("api/topics");

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakörök lekérésekor");

            return await response.Content.ReadFromJsonAsync<IEnumerable<Topic>>() ?? new List<Topic>();
        }

        // GET api/topics/{id}
        public async Task<Topic> GetTopicByIdAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/topics/{id}");

            await EnsureSuccessOrThrowAsync(response, $"Témakör lekérési hiba (id: {id})");

            return await response.Content.ReadFromJsonAsync<Topic>() ?? throw new InvalidOperationException("Üres válasz.");
        }

        // POST api/topics
        public async Task<Topic> AddTopicAsync(Topic topic)
        {
            PrepareHeaders();

            var requestBody = new
            {
                Title = topic.Title,
                SubjectId = topic.SubjectId
            };

            var response = await _httpClient.PostAsJsonAsync("api/topics", requestBody);

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakör létrehozásakor");

            return await response.Content.ReadFromJsonAsync<Topic>() ?? throw new InvalidOperationException("Szerver hiba.");
        }

        // PUT api/topics/{id}
        public async Task<Topic> UpdateTopicAsync(Topic topic)
        {
            PrepareHeaders();

            var requestBody = new
            {
                Id = topic.Id,
                Title = topic.Title,
                SubjectId = topic.SubjectId
            };

            var response = await _httpClient.PutAsJsonAsync($"api/topics/{topic.Id}", requestBody);

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakör frissítésekor");

            return await response.Content.ReadFromJsonAsync<Topic>() ?? throw new InvalidOperationException("Szerver hiba.");
        }

        // DELETE api/topics/{id}
        public async Task DeleteTopicAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.DeleteAsync($"api/topics/{id}");

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakör törlésekor");
        }

        // GET api/topics?subjectId={subjectId}
        public async Task<IEnumerable<Topic>> GetTopicBySubjectIdAsync(int subjectId)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/topics?subjectId={subjectId}");

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakörök lekérésekor");

            return await response.Content.ReadFromJsonAsync<IEnumerable<Topic>>() ?? new List<Topic>();
        }

        // GET api/topics
        public async Task<IEnumerable<Topic>> GetTopicsWithNotesAndQuestionsAsync()
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync("api/topics");

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakörök részletes lekérésekor");

            return await response.Content.ReadFromJsonAsync<IEnumerable<Topic>>() ?? new List<Topic>();
        }

        // GET api/topics/{id}
        public async Task<Topic> GetTopicWithNotesAndQuestionsAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/topics/{id}");

            await EnsureSuccessOrThrowAsync(response, $"Témakör lekérési hiba (id: {id})");

            return await response.Content.ReadFromJsonAsync<Topic>() ?? throw new InvalidOperationException("Üres válasz.");
        }

        // GET api/topics/{topicId}
        public async Task<Topic> GetTopicWithHierarchyAsync(int topicId)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/topics/{topicId}");

            await EnsureSuccessOrThrowAsync(response, $"Hierarchia lekérési hiba (id: {topicId})");

            return await response.Content.ReadFromJsonAsync<Topic>() ?? throw new InvalidOperationException("Üres válasz.");
        }

        public async Task<bool> TopicExistsAsync(string title, int subjectId, int? excludeId = null)
        {
            var topics = await GetTopicBySubjectIdAsync(subjectId);
            foreach (var topic in topics)
            {
                if (topic.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    if (excludeId.HasValue && topic.Id == excludeId.Value)
                        continue;

                    return true;
                }
            }
            return false;
        }
    }
}