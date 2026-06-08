using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MemoSphere.WPF.Services
{
    public abstract class BaseClientService
    {
        protected readonly HttpClient _httpClient;

        protected BaseClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Minden kérés előtt törli az előző fejlécet, és ha a felhasználó 
        /// be van jelentkezve, rásüti a legfrissebb Bearer JWT tokent a HttpClient-re.
        /// </summary>
        protected void PrepareHeaders()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrEmpty(ClientAuthService.CurrentToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", ClientAuthService.CurrentToken);
            }
        }

        /// <summary>
        /// Központosított hálózati hibakezelő.
        /// </summary>
        protected async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string contextMessage)
        {
            if (!response.IsSuccessStatusCode)
            {
                var serverError = await response.Content.ReadAsStringAsync();
                throw new Exception($"{contextMessage}. Szerver hibaüzenet: {serverError}");
            }
        }
    }
}