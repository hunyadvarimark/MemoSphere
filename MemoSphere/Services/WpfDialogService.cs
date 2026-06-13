using System.Windows;
using Core.Interfaces.Services;

namespace WPF.Services
{
    public class WpfDialogService : IDialogService
    {
        public void ShowMessage(string message, string title, MessageType type)
        {
            var image = type switch
            {
                MessageType.Warning => MessageBoxImage.Warning,
                MessageType.Error => MessageBoxImage.Error,
                _ => MessageBoxImage.Information
            };

            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }

        public DialogResult AskConfirmation(string message, string title)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes ? DialogResult.Yes : DialogResult.No;
        }
    }
}