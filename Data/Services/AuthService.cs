using Core.Interfaces.Services;
using Supabase.Gotrue;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Supabase.Gotrue.Constants;

namespace Data.Services
{
    public class AuthService : IAuthService
    {
        private readonly Supabase.Client _supabaseClient;

        public AuthService(Supabase.Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        /// <summary>
        /// Előállítja a Google OAuth URL-t a WebView számára.
        /// FONTOS: Előtte törli az esetleges régi session-t!
        /// </summary>
        public async Task<string> GetGoogleOAuthUrl()
        {
            try
            {
                // KRITIKUS: Töröljük a régi session-t, hogy új bejelentkezést kényszerítsünk
                Debug.WriteLine("=== Session törlése OAuth URL generálás előtt ===");

                var currentSession = _supabaseClient.Auth.CurrentSession;
                var currentUser = _supabaseClient.Auth.CurrentUser;

                Debug.WriteLine($"Session előtte: {(currentSession != null ? "VAN" : "NINCS")}");
                Debug.WriteLine($"User előtte: {(currentUser != null ? currentUser.Email : "NINCS")}");

                // SignOut hívása
                await _supabaseClient.Auth.SignOut();

                // Ellenőrizzük, hogy valóban törlődött-e
                currentSession = _supabaseClient.Auth.CurrentSession;
                currentUser = _supabaseClient.Auth.CurrentUser;

                Debug.WriteLine($"Session utána: {(currentSession != null ? "VAN" : "NINCS")}");
                Debug.WriteLine($"User utána: {(currentUser != null ? currentUser.Email : "NINCS")}");

                // Kis várakozás, hogy a Supabase kliens feldolgozza a SignOut-ot
                await Task.Delay(500);

                var redirectUrl = "memosphere://auth/callback";

                var options = new SignInOptions
                {
                    RedirectTo = redirectUrl
                };

                Debug.WriteLine("OAuth URL generálása...");
                var providerAuthState = await _supabaseClient.Auth.SignIn(Provider.Google, options);

                if (string.IsNullOrEmpty(providerAuthState?.Uri?.ToString()))
                {
                    Debug.WriteLine("Nem sikerült OAuth URL-t generálni");
                    return string.Empty;
                }

                Debug.WriteLine("OAuth URL generálva: " + providerAuthState.Uri);
                return providerAuthState.Uri.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Hiba az OAuth URL generálásakor: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Befejezi a bejelentkezést a WebView-ból kapott tokenekkel.
        /// </summary>
        public async Task<bool> CompleteGoogleSignInAsync(string accessToken, string refreshToken)
        {
            try
            {
                Debug.WriteLine("Session beállítása a kapott tokenekkel...");
                await _supabaseClient.Auth.SetSession(accessToken, refreshToken);

                Debug.WriteLine("Session sikeresen beállítva!");

                // Ellenőrizzük, hogy valóban be van-e állítva
                var currentUser = _supabaseClient.Auth.CurrentUser;
                if (currentUser != null)
                {
                    Debug.WriteLine($"Bejelentkezett felhasználó: {currentUser.Email}");
                    Debug.WriteLine($"User ID: {currentUser.Id}");
                    return true;
                }
                else
                {
                    Debug.WriteLine("HIBA: Session beállítva, de nincs CurrentUser!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Hiba a Google session beállításakor: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SignInWithMagicLinkAsync(string email)
        {
            try
            {
                var options = new SignInOptions
                {
                    RedirectTo = "memosphere://auth/callback"
                };

                await _supabaseClient.Auth.SignIn(email, options);
                Debug.WriteLine($"Magic link elküldve: {email}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Magic link hiba: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SignUpAsync(string email, string password)
        {
            try
            {
                Debug.WriteLine($"Regisztráció indítása: {email}");

                var response = await _supabaseClient.Auth.SignUp(email, password);

                if (response?.User != null)
                {
                    Debug.WriteLine($"Regisztráció sikeres. User ID: {response.User.Id}");
                    Debug.WriteLine($"Email confirmation required: {response.User.ConfirmationSentAt != null}");
                    return true;
                }

                Debug.WriteLine("Regisztráció sikertelen - nincs user a válaszban");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SignUp hiba: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> SignInAsync(string email, string password)
        {
            try
            {
                var response = await _supabaseClient.Auth.SignIn(email, password);

                if (response?.User != null)
                {
                    Debug.WriteLine($"Email/Password bejelentkezés sikeres: {response.User.Email}");
                    Debug.WriteLine($"User ID: {response.User.Id}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SignIn hiba: {ex.Message}");
                return false;
            }
        }

        public Guid GetCurrentUserId()
        {
            var userId = _supabaseClient.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("Nincs bejelentkezett felhasználó.");
            }
            return Guid.Parse(userId);
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var session = _supabaseClient.Auth.CurrentSession;

            if (session == null)
            {
                Debug.WriteLine("IsAuthenticated: Nincs session");
                return false;
            }

            // Ellenőrizzük a lejárati időt
            var expiresAt = session.ExpiresAt();
            if (expiresAt <= DateTime.Now)
            {
                Debug.WriteLine($"IsAuthenticated: Session lejárt ({expiresAt})");
                return false;
            }

            Debug.WriteLine("IsAuthenticated: Van érvényes session");
            return await Task.FromResult(true);
        }

        public string? GetCurrentUserEmail()
        {
            return _supabaseClient.Auth.CurrentUser?.Email;
        }

        public async Task SignOutAsync()
        {
            Debug.WriteLine("SignOut meghívva");
            await _supabaseClient.Auth.SignOut();
            Debug.WriteLine("SignOut befejezve");
        }
    }
}