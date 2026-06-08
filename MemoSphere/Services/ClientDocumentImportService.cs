using Core.Interfaces.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MemoSphere.WPF.Services
{
    public class ClientDocumentImportService : BaseClientService, IDocumentImportService
    {
        public ClientDocumentImportService(HttpClient httpClient) : base(httpClient)
        {
        }

        private record DocumentImportResponse(string Content);

        public async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("A feldolgozandó PDF fájl nem található.", filePath);

            PrepareHeaders();

            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var streamContent = new StreamContent(fileStream);

            content.Add(streamContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync("api/documentimports/extract-pdf", content);

            await EnsureSuccessOrThrowAsync(response, "Hiba a PDF szöveg kinyerése során");

            var result = await response.Content.ReadFromJsonAsync<DocumentImportResponse>();

            return result?.Content ?? string.Empty;
        }
        
        public bool IsPdfFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            return filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        }
    }
}