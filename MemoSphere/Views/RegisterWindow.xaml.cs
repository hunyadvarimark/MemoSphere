using Core.Interfaces.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MemoSphere.WPF.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly IAuthService _authService;

        public RegisterWindow(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService;

            // Enter lenyomásra is regisztráljon
            ConfirmPasswordBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    RegisterButton_Click(s, e);
                }
            };
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoading(true);
                HideError();

                var email = EmailTextBox.Text.Trim();
                var password = PasswordBox.Password;
                var confirmPassword = ConfirmPasswordBox.Password;

                // Validáció
                if (string.IsNullOrEmpty(email))
                {
                    ShowError("Kérlek add meg az email címed!");
                    return;
                }

                if (!IsValidEmail(email))
                {
                    ShowError("Érvénytelen email cím!");
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    ShowError("Kérlek add meg a jelszavad!");
                    return;
                }

                if (password.Length < 6)
                {
                    ShowError("A jelszónak legalább 6 karakter hosszúnak kell lennie!");
                    return;
                }

                if (string.IsNullOrEmpty(confirmPassword))
                {
                    ShowError("Kérlek erősítsd meg a jelszavad!");
                    return;
                }

                if (password != confirmPassword)
                {
                    ShowError("A két jelszó nem egyezik!");
                    return;
                }

                // Regisztráció
                Debug.WriteLine($"Regisztráció indítása: {email}");
                var success = await _authService.SignUpAsync(email, password);

                if (success)
                {
                    MessageBox.Show(
                        "Sikeres regisztráció! Kérlek ellenőrizd az email fiókodat a megerősítő linkért.\n\n" +
                        "A megerősítés után bejelentkezhetsz a fiókodba.",
                        "Regisztráció sikeres",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Vissza a login ablakhoz
                    var loginWindow = new LoginWindow(_authService, Application.Current.MainWindow as MainWindow);
                    loginWindow.Show();
                    this.Close();
                }
                else
                {
                    ShowError("Regisztráció sikertelen. Lehet, hogy ez az email már használatban van.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Regisztráció hiba: {ex.Message}");
                ShowError($"Hiba történt: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void SignInLink_Click(object sender, MouseButtonEventArgs e)
        {
            // Vissza a login ablakhoz
            var loginWindow = new LoginWindow(_authService, Application.Current.MainWindow as MainWindow);
            loginWindow.Show();
            this.Close();
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
            RegisterButton.IsEnabled = !isLoading;
            EmailTextBox.IsEnabled = !isLoading;
            PasswordBox.IsEnabled = !isLoading;
            ConfirmPasswordBox.IsEnabled = !isLoading;
        }
    }
}