namespace Core.Interfaces.Services
{
    public enum MessageType { Info, Warning, Error }
    public enum DialogResult { Yes, No }

    public interface IDialogService
    {
        void ShowMessage(string message, string title, MessageType type);

        DialogResult AskConfirmation(string message, string title);
    }
}