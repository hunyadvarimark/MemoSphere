using Core.Entities;
using Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MemoSphere.WPF.Services
{
    public class ClientNoteService : BaseClientService, INoteService
    {
        public ClientNoteService(HttpClient httpClient) : base(httpClient)
        {
        }

        // GET api/notes?topicId={topicId}
        public async Task<IEnumerable<Note>> GetNotesByTopicIdAsync(int topicId)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/notes?topicId={topicId}");

            await EnsureSuccessOrThrowAsync(response, "Hiba a jegyzetek lekérésekor");

            return await response.Content.ReadFromJsonAsync<IEnumerable<Note>>() ?? new List<Note>();
        }

        // GET api/notes/{id}
        public async Task<Note> GetNoteByIdAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.GetAsync($"api/notes/{id}");

            await EnsureSuccessOrThrowAsync(response, $"Jegyzet lekérési hiba (id: {id})");

            return await response.Content.ReadFromJsonAsync<Note>()
                   ?? throw new InvalidOperationException("Üres válasz érkezett a szervertől.");
        }

        // POST api/notes
        public async Task<Note> AddNoteAsync(Note note)
        {
            PrepareHeaders();

            var requestBody = new
            {
                Title = note.Title,
                Content = note.Content,
                TopicId = note.TopicId
            };

            var response = await _httpClient.PostAsJsonAsync("api/notes", requestBody);

            await EnsureSuccessOrThrowAsync(response, "Hiba a jegyzet létrehozásakor");

            return await response.Content.ReadFromJsonAsync<Note>()
                   ?? throw new InvalidOperationException("Nem sikerült beolvasni a létrehozott jegyzetet.");
        }

        // PUT api/notes/{id}
        public async Task<Note> UpdateNoteAsync(Note note)
        {
            PrepareHeaders();

            var requestBody = new
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content,
                TopicId = note.TopicId
            };

            var response = await _httpClient.PutAsJsonAsync($"api/notes/{note.Id}", requestBody);

            await EnsureSuccessOrThrowAsync(response, $"Hiba a jegyzet frissítésekor (id: {note.Id})");

            return await response.Content.ReadFromJsonAsync<Note>()
                   ?? throw new InvalidOperationException("Nem sikerült beolvasni a frissített jegyzetet.");
        }

        // DELETE api/notes/{id}
        public async Task DeleteNoteAsync(int id)
        {
            PrepareHeaders();

            var response = await _httpClient.DeleteAsync($"api/notes/{id}");

            await EnsureSuccessOrThrowAsync(response, $"Hiba a jegyzet törlésekor (id: {id})");
        }
    }
}