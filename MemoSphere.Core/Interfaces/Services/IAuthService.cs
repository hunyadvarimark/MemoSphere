using System;
using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<bool> SignUpAsync(string email, string password);
        Task<string?> SignInAsync(string email, string password);
        Task SignOutAsync();
        Guid GetCurrentUserId();
        Task<bool> IsAuthenticatedAsync();
        string? GetCurrentUserEmail();

        // Ezeket egyelőre meghagyjuk üresen, vagy később implementáljuk egyedi szolgáltatóval
        Task<string> GetGoogleOAuthUrl();
        Task<bool> CompleteGoogleSignInAsync(string accessToken, string refreshToken);
        Task<bool> SignInWithMagicLinkAsync(string email);
    }
}