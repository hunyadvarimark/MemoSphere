namespace Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<bool> SignUpAsync(string email, string password);
        Task<bool> SignInAsync(string email, string password);

        Task<string> GetGoogleOAuthUrl();

        Task<bool> CompleteGoogleSignInAsync(string accessToken, string refreshToken);

        Task<bool> SignInWithMagicLinkAsync(string email);
        Task SignOutAsync();
        Guid GetCurrentUserId();
        Task<bool> IsAuthenticatedAsync();
        string? GetCurrentUserEmail();
    }
}