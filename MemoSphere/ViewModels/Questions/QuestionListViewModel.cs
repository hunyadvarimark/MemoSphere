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
                    "Generáljak kérdéseket minden típusból?\n\n" +
                    "• Feleletválasztós\n" +
                    "• Igaz/Hamis\n" +
                    "• Rövid válasz\n\n" +
                    "A kérdések száma a jegyzet tartalmától függ.",
                    "Kérdésgenerálás",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsGenerating = true;
                var tasks = new[]
                {
            _questionService.GenerateAndSaveQuestionsAsync(_currentNoteId, QuestionType.MultipleChoice),
            _questionService.GenerateAndSaveQuestionsAsync(_currentNoteId, QuestionType.TrueFalse),
            _questionService.GenerateAndSaveQuestionsAsync(_currentNoteId, QuestionType.ShortAnswer)
        };

                var results = await Task.WhenAll(tasks);

                if (results.All(r => r))
                {
                    MessageBox.Show("Kérdések sikeresen generálva!", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadQuestionsAsync(_currentNoteId);
                }
                else
                {
                    MessageBox.Show("Nem minden kérdéstípus generálása sikerült.", "Figyelmeztetés",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
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