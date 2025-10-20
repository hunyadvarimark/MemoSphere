using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;
using WPF.Utilities;

namespace WPF.ViewModels.Questions
{
    public class QuestionListViewModel : BaseViewModel
    {
        private readonly IQuestionService _questionService;
        private int _currentNoteId;

        private bool _isGenerating;
        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                if (SetProperty(ref _isGenerating, value))
                {
                    GenerateQuestionsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<Question> Questions { get; } = new();
        public AsyncCommand<object> GenerateQuestionsCommand { get; }

        public QuestionListViewModel(IQuestionService questionService)
        {
            _questionService = questionService;
            GenerateQuestionsCommand = new AsyncCommand<object>(GenerateQuestionsForNoteAsync, CanGenerateQuestions);
        }

        public async Task LoadQuestionsAsync(int noteId)
        {
            _currentNoteId = noteId;
            Questions.Clear();
            if (noteId > 0)
            {
                var questions = await _questionService.GetQuestionsForNoteAsync(noteId); // Feltételezve, hogy van ilyen metódusod
                foreach (var q in questions)
                {
                    Questions.Add(q);
                }
            }
            GenerateQuestionsCommand.RaiseCanExecuteChanged(); // Frissítjük a gomb állapotát
        }

        private bool CanGenerateQuestions(object parameter)
        {
            return _currentNoteId > 0;
        }

        public void ClearQuestions()
        {
            Questions.Clear();
        }
        public async Task GenerateQuestionsForNoteAsync(object parameter)
        {
            if (_currentNoteId <= 0) return;

            try
            {
                var result = MessageBox.Show(
                    "Milyen típusú kérdéseket generáljak?\n\n" +
                    "YES = Feleletválasztós\n" +
                    "NO = Igaz/Hamis\n" +
                    "CANCEL = Rövid válasz",
                    "Kérdéstípus kiválasztása",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                QuestionType type;
                switch (result)
                {
                    case MessageBoxResult.Yes: type = QuestionType.MultipleChoice; break;
                    case MessageBoxResult.No: type = QuestionType.TrueFalse; break;
                    case MessageBoxResult.Cancel: type = QuestionType.ShortAnswer; break;
                    default: return;
                }

                bool success = await _questionService.GenerateAndSaveQuestionsAsync(_currentNoteId, type);

                if (success)
                {
                    MessageBox.Show($"{GetQuestionTypeDisplayName(type)} kérdések sikeresen generálva!", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadQuestionsAsync(_currentNoteId);
                }
                else
                {
                    MessageBox.Show("Nem sikerült kérdéseket generálni...", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a kérdésgenerálás során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private string GetQuestionTypeDisplayName(QuestionType type)
        {
            return type switch
            {
                QuestionType.MultipleChoice => "Feleletválasztós",
                QuestionType.TrueFalse => "Igaz/Hamis",
                QuestionType.ShortAnswer => "Rövid válasz",
                _ => type.ToString()
            };
        }

    }
}