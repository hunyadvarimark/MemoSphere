using Core.Interfaces.Services;
using System;
using System.Threading.Tasks;

namespace MemoSphere.Api.Services
{
    // Ez a pehelysúlyú osztály beugrik a régi Supabase-es AuthService helyére az API-ban
    public class ApiAuthMockService : IAuthService
    {
        // Fix Teszt Felhasználó ID, amit az adatbázis migrációnál is beállítottunk
        private static readonly Guid MockUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public Guid GetCurrentUserId()
        {
            return MockUserId;
        }

        public string GetCurrentUserEmail()
        {
            return "fejleszto.teszt@memosphere.local";
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            return Task.FromResult(true);
        }

        public Task<bool> CompleteGoogleSignInAsync(string accessToken, string refreshToken) => Task.FromResult(true);
        public Task SignOutAsync() => Task.CompletedTask;

        public Task<bool> SignUpAsync(string email, string password)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignInAsync(string email, string password)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetGoogleOAuthUrl()
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignInWithMagicLinkAsync(string email)
        {
            throw new NotImplementedException();
        }
    }
}