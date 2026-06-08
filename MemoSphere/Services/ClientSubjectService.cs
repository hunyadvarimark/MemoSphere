using Core.Entities;
using Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MemoSphere.WPF.Services
{
    public class ClientSubjectService : BaseClientService, ISubjectService
    {
        public ClientSubjectService(HttpClient httpClient) : base(httpClient)
        {
        }

        // GET api/subjects
        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync("api/subjects");

            await EnsureSuccessOrThrowAsync(response, "Hiba a tantárgyak lekérésekor");

            return await response.Content.ReadFromJsonAsync<IEnumerable<Subject>>() ?? new List<Subject>();
        }

        // GET api/subjects/{id}
        public async Task<Subject> GetSubjectByIdAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/subjects/{id}");

            await EnsureSuccessOrThrowAsync(response, $"Tantárgy lekérési hiba (id: {id})");

            return await response.Content.ReadFromJsonAsync<Subject>()
                   ?? throw new InvalidOperationException("Üres válasz érkezett a szervertől.");
        }

        // POST api/subjects
        public async Task<Subject> AddSubjectAsync(Subject subject)
        {
            PrepareHeaders();

            var requestBody = new
            {
                Title = subject.Title
            };

            var response = await _httpClient.PostAsJsonAsync("api/subjects", requestBody);

            await EnsureSuccessOrThrowAsync(response, "Hiba a tantárgy létrehozásakor");

            return await response.Content.ReadFromJsonAsync<Subject>()
                   ?? throw new InvalidOperationException("Nem sikerült beolvasni a létrehozott tantárgyat.");
        }

        // PUT api/subjects/{id}
        public async Task<Subject> UpdateSubjectAsync(Subject subject)
        {
            PrepareHeaders();

            var requestBody = new
            {
                Id = subject.Id,
                Title = subject.Title
            };

            var response = await _httpClient.PutAsJsonAsync($"api/subjects/{subject.Id}", requestBody);

            await EnsureSuccessOrThrowAsync(response, $"Hiba a tantárgy frissítésekor (id: {subject.Id})");

            return await response.Content.ReadFromJsonAsync<Subject>()
                   ?? throw new InvalidOperationException("Nem sikerült beolvasni a frissített tantárgyat.");
        }

        // DELETE api/subjects/{id}
        public async Task DeleteSubjectAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.DeleteAsync($"api/subjects/{id}");

            await EnsureSuccessOrThrowAsync(response, $"Hiba a tantárgy törlésekor (id: {id})");
        }

        // GET api/subjects
        public async Task<IEnumerable<Subject>> GetAllSubjectsWithTopicsAsync()
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync("api/subjects");

            await EnsureSuccessOrThrowAsync(response, "Hiba a tantárgyak és témakörök lekérésekor");

            return await response.Content.ReadFromJsonAsync<IEnumerable<Subject>>() ?? new List<Subject>();
        }

        // POST api/subjects
        public async Task<Subject> AddSubjectAsync(string title)
        {
            PrepareHeaders();

            var requestBody = new { Title = title };
            var response = await _httpClient.PostAsJsonAsync("api/subjects", requestBody);

            await EnsureSuccessOrThrowAsync(response, "Hiba a tantárgy hozzáadásakor");

            return await response.Content.ReadFromJsonAsync<Subject>()
                   ?? throw new InvalidOperationException("Nem sikerült beolvasni a létrehozott tantárgyat.");
        }
        public async Task<bool> SubjectExistsAsync(string title, int? excludeId = null)
        {
            var subjects = await GetAllSubjectsAsync();
            foreach (var subject in subjects)
            {
                if (subject.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    if (excludeId.HasValue && subject.Id == excludeId.Value)
                        continue;

                    return true;
                }
            }
            return false;
        }

        // GET api/subjects/{id}
        public async Task<Subject> GetSubjectWithHierarchyAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/subjects/{id}");

            await EnsureSuccessOrThrowAsync(response, $"Hiba a hierarchia lekérésekor (id: {id})");

            return await response.Content.ReadFromJsonAsync<Subject>()
                   ?? throw new InvalidOperationException("Üres válasz érkezett a szervertől.");
        }
    }
}