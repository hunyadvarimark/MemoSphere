// MemoSphere.WPF.Views.LoginWindow.xaml.cs

using Core.Interfaces.Services;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MemoSphere.WPF.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthService _authService;
        private readonly MainWindow _mainWindow;
        private Window? _webViewWindow;

        public LoginWindow(IAuthService authService, MainWindow mainWindow)
        {
            InitializeComponent();
            _authService = authService;
            _mainWindow = mainWindow;

            // Enter lenyomásra is bejelentkezzen
            PasswordBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    SignInButton_Click(s, e);
                }
            };
        }

        private async void GoogleSignInButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("GoogleSignInButton_Click meghívva");

            try
            {
                HideError();

                // 1. OAuth URL lekérése
                Debug.WriteLine("OAuth URL lekérése...");
                var oauthUrl = await _authService.GetGoogleOAuthUrl();
                Debug.WriteLine($"OAuth URL: {oauthUrl}");

                if (string.IsNullOrEmpty(oauthUrl))
                {
                    ShowError("Nem sikerült előkészíteni a Google bejelentkezést.");
                    return;
                }

                // 2. WebView ablak létrehozása
                Debug.WriteLine("WebView ablak létrehozása...");
                _webViewWindow = new Window
                {
                    Title = "Sign in with Google",
                    Width = 500,
                    Height = 650,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
                _webViewWindow.Content = webView;

                // WebView bezárása esetén (ha a felhasználó zárja be)
                _webViewWindow.Closed += (s, args) =>
                {
                    Debug.WriteLine("WebView ablak bezárva");
                    _webViewWindow = null;
                };

                // 3. WebView inicializálás ELŐTT megjelenítjük az ablakot
                Debug.WriteLine("WebView ablak megjelenítése...");
                _webViewWindow.Show(); // ShowDialog helyett Show, hogy ne blokkoljon

                Debug.WriteLine("WebView inicializálása...");
                try
                {
                    var env = await CoreWebView2Environment.CreateAsync(
                        userDataFolder: System.IO.Path.Combine(
                            System.IO.Path.GetTempPath(),
                            "MemoSphere_WebView2_" + Guid.NewGuid()
                        )
                    );

                    await webView.EnsureCoreWebView2Async(env);
                    Debug.WriteLine("WebView inicializálva (új környezet, tiszta cache)");

                    // Cookie-k törlése az adott domain-ről
                    var cookieManager = webView.CoreWebView2.CookieManager;
                    var cookies = await cookieManager.GetCookiesAsync("https://accounts.google.com");
                    foreach (var cookie in cookies)
                    {
                        cookieManager.DeleteCookie(cookie);
                    }
                    Debug.WriteLine("Google cookie-k törölve");
                }
                catch (Exception initEx)
                {
                    Debug.WriteLine($"WebView inicializálási hiba: {initEx.Message}");
                    MessageBox.Show(
                        "A WebView2 inicializálása sikertelen. Győződj meg róla, hogy a Microsoft Edge WebView2 Runtime telepítve van.\n\n" +
                        "Letöltés: https://go.microsoft.com/fwlink/p/?LinkId=2124703\n\n" +
                        $"Hiba: {initEx.Message}",
                        "WebView2 Runtime hiányzik",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    _webViewWindow?.Close();
                    _webViewWindow = null;
                    return;
                }

                // 4. Navigation event handler - figyeljük az URL változásokat
                webView.NavigationStarting += async (s, args) =>
                {
                    var url = args.Uri;
                    Debug.WriteLine($"Navigation: {url}");

                    // 5. Ellenőrizzük, hogy a mi egyedi callback URL-ünk-e
                    if (url.StartsWith("memosphere://auth/callback"))
                    {
                        Debug.WriteLine("Callback URL elkapva!");

                        // Megállítjuk a navigációt
                        args.Cancel = true;

                        var uri = new Uri(url);
                        var fragment = uri.Fragment.TrimStart('#');

                        if (string.IsNullOrEmpty(fragment))
                        {
                            Debug.WriteLine("Callback URL, de nincs fragment.");
                            ShowError("Hiba a Google bejelentkezés során (hiányzó token).");
                            _webViewWindow?.Close();
                            return;
                        }

                        // 6. Tokenek kinyerése a fragment-ből
                        var parameters = System.Web.HttpUtility.ParseQueryString(fragment);
                        var accessToken = parameters["access_token"];
                        var refreshToken = parameters["refresh_token"];

                        Debug.WriteLine($"Access Token: {(string.IsNullOrEmpty(accessToken) ? "NINCS" : "VAN")}");
                        Debug.WriteLine($"Refresh Token: {(string.IsNullOrEmpty(refreshToken) ? "NINCS" : "VAN")}");

                        if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
                        {
                            // WebView bezárása
                            _webViewWindow?.Close();
                            _webViewWindow = null;

                            // Loading indikátor megjelenítése
                            Dispatcher.Invoke(() => ShowLoading(true));

                            // 7. Session beállítása
                            Debug.WriteLine("Session beállítása...");
                            var success = await _authService.CompleteGoogleSignInAsync(accessToken, refreshToken);

                            Dispatcher.Invoke(() => ShowLoading(false));

                            if (success)
                            {
                                Debug.WriteLine("Sikeres bejelentkezés!");
                                Dispatcher.Invoke(async () =>
                                {
                                    await _mainWindow.LoadDataAsync();
                                    _mainWindow.Show();
                                    this.Close();
                                });
                            }
                            else
                            {
                                Debug.WriteLine("Session beállítása sikertelen");
                                Dispatcher.Invoke(() => ShowError("Hiba történt a bejelentkezés befejezésekor."));
                            }
                        }
                        else
                        {
                            // Hiba kezelése a fragment-ből
                            var error = parameters["error"];
                            var errorDescription = parameters["error_description"];
                            Debug.WriteLine($"Callback hiba: {error} - {errorDescription}");
                            Dispatcher.Invoke(() => ShowError($"Hiba a Google bejelentkezés során: {errorDescription ?? error}"));
                            _webViewWindow?.Close();
                        }
                    }
                };

                // 8. OAuth URL betöltése
                Debug.WriteLine("OAuth URL betöltése a WebView-ba...");
                webView.CoreWebView2.Navigate(oauthUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Google OAuth WebView hiba: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                ShowError($"Hiba történt: {ex.Message}");

                _webViewWindow?.Close();
                _webViewWindow = null;
            }
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoading(true);
                HideError();

                var email = EmailTextBox.Text.Trim();
                var password = PasswordBox.Password;

                // Validáció
                if (string.IsNullOrEmpty(email))
                {
                    ShowError("Kérlek add meg az email címed!");
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    ShowError("Kérlek add meg a jelszavad!");
                    return;
                }

                if (!IsValidEmail(email))
                {
                    ShowError("Érvénytelen email cím!");
                    return;
                }

                // Bejelentkezés
                var success = await _authService.SignInAsync(email, password);

                if (success)
                {
                    // Sikeres bejelentkezés
                    await _mainWindow.LoadDataAsync();
                    _mainWindow.Show();
                    this.Close();
                }
                else
                {
                    ShowError("Hibás email vagy jelszó!");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Hiba történt: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async void SignUpLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                ShowLoading(true);
                HideError();

                var email = EmailTextBox.Text.Trim();
                var password = PasswordBox.Password;

                // Validáció
                if (string.IsNullOrEmpty(email))
                {
                    ShowError("Kérlek add meg az email címed!");
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    ShowError("Kérlek add meg a jelszavad!");
                    return;
                }

                if (!IsValidEmail(email))
                {
                    ShowError("Érvénytelen email cím!");
                    return;
                }

                if (password.Length < 6)
                {
                    ShowError("A jelszónak legalább 6 karakter hosszúnak kell lennie!");
                    return;
                }

                // Regisztráció
                var success = await _authService.SignUpAsync(email, password);

                if (success)
                {
                    MessageBox.Show(
                        "Sikeres regisztráció! Kérlek ellenőrizd az email fiókodat a megerősítő linkért.",
                        "Regisztráció sikeres",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Email és jelszó törlése
                    EmailTextBox.Text = "";
                    PasswordBox.Password = "";
                }
                else
                {
                    ShowError("Regisztráció sikertelen. Lehet, hogy ez az email már használatban van.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Hiba történt: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        private void ShowLoading(bool isLoading)
        {
            LoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            GoogleSignInButton.IsEnabled = !isLoading;
            EmailTextBox.IsEnabled = !isLoading;
            PasswordBox.IsEnabled = !isLoading;
        }
    }
}