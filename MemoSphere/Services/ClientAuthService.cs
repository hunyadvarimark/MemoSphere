using Core.Interfaces.Services;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemoSphere.WPF.Services
{
    public class ClientAuthService : IAuthService
    {
        private readonly HttpClient _httpClient;

        public static string? CurrentToken { get; private set; }

        public ClientAuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> SignInAsync(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { Email = email, Password = password });

            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            CurrentToken = result?.Token;

            if (!string.IsNullOrEmpty(CurrentToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentToken);
            }

            return CurrentToken;
        }

        public async Task<bool> SignUpAsync(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", new { Email = email, Password = password });
            return response.IsSuccessStatusCode;
        }

        public Guid GetCurrentUserId()
        {
            if (string.IsNullOrEmpty(CurrentToken)) return Guid.Empty;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(CurrentToken);
            var userIdStr = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;

            return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
        }

        public string? GetCurrentUserEmail()
        {
            if (string.IsNullOrEmpty(CurrentToken)) return null;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(CurrentToken);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        public Task<bool> IsAuthenticatedAsync() => Task.FromResult(!string.IsNullOrEmpty(CurrentToken));

        public Task SignOutAsync()
        {
            CurrentToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return Task.CompletedTask;
        }

        public Task<string> GetGoogleOAuthUrl() => Task.FromResult(string.Empty);
        public Task<bool> CompleteGoogleSignInAsync(string accessToken, string refreshToken) => Task.FromResult(false);
        public Task<bool> SignInWithMagicLinkAsync(string email) => Task.FromResult(false);
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}