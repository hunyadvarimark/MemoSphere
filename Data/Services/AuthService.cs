using Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Data.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser<Guid>> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<IdentityUser<Guid>> userManager,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> SignUpAsync(string email, string password)
        {
            var user = new IdentityUser<Guid>
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            return result.Succeeded;
        }

        public async Task<string?> SignInAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid) return null;

            return GenerateJwtToken(user);
        }

        public Guid GetCurrentUserId()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr))
            {
                throw new InvalidOperationException("Nincs bejelentkezett felhasználó a HTTP kontextusban.");
            }

            return Guid.Parse(userIdStr);
        }

        public string? GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            var isAuthenticated = _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            return Task.FromResult(isAuthenticated);
        }

        public Task SignOutAsync()
        {
            return Task.CompletedTask;
        }

        private string GenerateJwtToken(IdentityUser<Guid> user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            var secretKey = _configuration["JwtSettings:SecretKey"]
                ?? "NAGYON_TITKOS_MOCK_KULCS_AMIT_KI_KELL_CSERELNI_MAJD";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Egyelőre üresen hagyott Supabase specifikus metódusok
        public Task<string> GetGoogleOAuthUrl() => throw new NotImplementedException();
        public Task<bool> CompleteGoogleSignInAsync(string accessToken, string refreshToken) => throw new NotImplementedException();
        public Task<bool> SignInWithMagicLinkAsync(string email) => throw new NotImplementedException();
    }
}