using Core.Enums;
using System.Collections.ObjectModel;
using WPF.ViewModels;

public class QuestionViewModel : BaseViewModel
{
    public int Id { get; set; }
    public string Text { get; set; }
    public string CorrectAnswer { get; set; }
    public QuestionType Type { get; set; }
    public ObservableCollection<string> Options { get; set; }
}
