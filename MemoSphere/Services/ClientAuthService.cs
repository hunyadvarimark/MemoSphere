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
        private static string? _currentToken;

        public ClientAuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> SignInAsync(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { Email = email, Password = password });

            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            _currentToken = result?.Token;

            if (!string.IsNullOrEmpty(_currentToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);
            }

            return _currentToken;
        }

        public async Task<bool> SignUpAsync(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", new { Email = email, Password = password });
            return response.IsSuccessStatusCode;
        }

        public Guid GetCurrentUserId()
        {
            if (string.IsNullOrEmpty(_currentToken)) return Guid.Empty;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(_currentToken);
            var userIdStr = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;

            return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
        }

        public string? GetCurrentUserEmail()
        {
            if (string.IsNullOrEmpty(_currentToken)) return null;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(_currentToken);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        public Task<bool> IsAuthenticatedAsync() => Task.FromResult(!string.IsNullOrEmpty(_currentToken));

        public Task SignOutAsync()
        {
            _currentToken = null;
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