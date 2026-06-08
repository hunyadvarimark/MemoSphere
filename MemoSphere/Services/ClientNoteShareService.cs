using Core.Interfaces.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MemoSphere.WPF.Services
{
    public class ClientNoteShareService : BaseClientService, INoteShareService
    {
        public ClientNoteShareService(HttpClient httpClient) : base(httpClient)
        {
        }

        // ==========================================
        // EXPORTÁLÁSI FUNKCIÓK (Fájl letöltése a szerverről)
        // ==========================================

        // GET api/datashares/export/note/{noteId}
        public async Task ExportNoteToFileAsync(int noteId, string filePath)
        {
            PrepareHeaders();

            var url = $"api/datashares/export/note/{noteId}";
            var response = await _httpClient.GetAsync(url);

            await EnsureSuccessOrThrowAsync(response, $"Hiba a jegyzet exportálásakor (id: {noteId})");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, bytes);
        }

        // GET api/datashares/export/topic/{topicId}
        public async Task ExportTopicToFileAsync(int topicId, string filePath)
        {
            PrepareHeaders();

            var url = $"api/datashares/export/topic/{topicId}";
            var response = await _httpClient.GetAsync(url);

            await EnsureSuccessOrThrowAsync(response, $"Hiba a témakör exportálásakor (id: {topicId})");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, bytes);
        }

        // GET api/datashares/export/subject/{subjectId}
        public async Task ExportSubjectToFileAsync(int subjectId, string filePath)
        {
            PrepareHeaders();

            var url = $"api/datashares/export/subject/{subjectId}";
            var response = await _httpClient.GetAsync(url);

            await EnsureSuccessOrThrowAsync(response, $"Hiba a tantárgy exportálásakor (id: {subjectId})");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, bytes);
        }

        // ==========================================
        // IMPORTÁLÁSI FUNKCIÓK (Fájl feltöltése Multipart Form-Data segítségével)
        // ==========================================

        // POST api/datashares/import/note?targetTopicId={targetTopicId}
        public async Task ImportNoteFromFileAsync(string filePath, int targetTopicId)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Az importálandó fájl nem található.", filePath);

            PrepareHeaders();

            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var streamContent = new StreamContent(fileStream);

            content.Add(streamContent, "file", Path.GetFileName(filePath));

            var url = $"api/datashares/import/note?targetTopicId={targetTopicId}";
            var response = await _httpClient.PostAsync(url, content);

            await EnsureSuccessOrThrowAsync(response, "Hiba a jegyzet importálásakor");
        }

        // POST api/datashares/import/topic?targetSubjectId={targetSubjectId}
        public async Task ImportTopicFromFileAsync(string filePath, int targetSubjectId)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Az importálandó fájl nem található.", filePath);

            PrepareHeaders();

            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var streamContent = new StreamContent(fileStream);

            content.Add(streamContent, "file", Path.GetFileName(filePath));

            var url = $"api/datashares/import/topic?targetSubjectId={targetSubjectId}";
            var response = await _httpClient.PostAsync(url, content);

            await EnsureSuccessOrThrowAsync(response, "Hiba a témakör importálásakor");
        }

        // POST api/datashares/import/subject
        public async Task ImportSubjectFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Az importálandó fájl nem található.", filePath);

            PrepareHeaders();

            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var streamContent = new StreamContent(fileStream);

            content.Add(streamContent, "file", Path.GetFileName(filePath));

            var url = "api/datashares/import/subject";
            var response = await _httpClient.PostAsync(url, content);

            await EnsureSuccessOrThrowAsync(response, "Hiba a tantárgy importálásakor");
        }
    }
}