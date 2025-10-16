using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;
using WPF.Utilities;

namespace WPF.ViewModels.Questions
{
    public class QuestionDetailViewModel : BaseViewModel
    {
        private readonly IQuestionService _questionService; // ✅ Tároljuk!
        private Question _currentQuestion;
        private string _questionText = string.Empty;
        private QuestionType _questionType;
        private string _correctAnswer = string.Empty;
        private ObservableCollection<string> _options = new();
        private int _topicId;

        public string QuestionText
        {
            get => _questionText;
            set
            {
                if (SetProperty(ref _questionText, value))
                {
                    SaveQuestionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public QuestionType QuestionType
        {
            get => _questionType;
            set
            {
                if (SetProperty(ref _questionType, value))
                {
                    OnQuestionTypeChanged();
                    SaveQuestionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string CorrectAnswer
        {
            get => _correctAnswer;
            set
            {
                if (SetProperty(ref _correctAnswer, value))
                {
                    SaveQuestionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<string> Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

        public int TopicId
        {
            get => _topicId;
            set
            {
                if (SetProperty(ref _topicId, value))
                {
                    SaveQuestionCommand.RaiseCanExecuteChanged();
                    GenerateQuestionsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public AsyncCommand<object> SaveQuestionCommand { get; }
        public AsyncCommand<QuestionType> GenerateQuestionsCommand { get; } // ✅ Ez lesz inicializálva

        public event Action<Question> QuestionSavedRequested;

        public QuestionDetailViewModel(IQuestionService questionService)
        {
            _questionService = questionService; // ✅ Eltároljuk!

            SaveQuestionCommand = new AsyncCommand<object>(SaveQuestionRequestedAsync, CanSaveQuestion);
            GenerateQuestionsCommand = new AsyncCommand<QuestionType>(GenerateQuestionsAsync, CanGenerateQuestions); // ✅ Inicializálás
        }

        public void ResetState(int topicId)
        {
            _currentQuestion = null;
            QuestionText = string.Empty;
            QuestionType = QuestionType.MultipleChoice;
            CorrectAnswer = string.Empty;
            Options.Clear();
            TopicId = topicId;
        }

        public void LoadQuestion(Question questionToEdit)
        {
            _currentQuestion = questionToEdit;
            QuestionText = questionToEdit?.Text ?? string.Empty;
            QuestionType = questionToEdit?.QuestionType ?? QuestionType.MultipleChoice;
            CorrectAnswer = questionToEdit?.Answers?.FirstOrDefault(a => a.IsCorrect)?.Text ?? string.Empty;
            TopicId = questionToEdit?.TopicId ?? 0;

            Options.Clear();
            if (questionToEdit?.Answers != null)
            {
                foreach (var answer in questionToEdit.Answers.Where(a => !a.IsCorrect))
                {
                    Options.Add(answer.Text);
                }
            }

            SaveQuestionCommand.RaiseCanExecuteChanged();
        }

        private void OnQuestionTypeChanged()
        {
            if (QuestionType != QuestionType.MultipleChoice)
            {
                Options.Clear();
            }
        }

        private bool CanSaveQuestion(object parameter)
        {
            bool basicValid = !string.IsNullOrWhiteSpace(QuestionText)
                && !string.IsNullOrWhiteSpace(CorrectAnswer)
                && TopicId > 0;

            if (QuestionType == QuestionType.MultipleChoice)
            {
                return basicValid && Options.Count >= 2;
            }

            return basicValid;
        }

        private async Task SaveQuestionRequestedAsync(object parameter)
        {
            if (!CanSaveQuestion(null)) return;

            Question questionToSave = _currentQuestion ?? new Question { TopicId = TopicId };
            questionToSave.Text = QuestionText;
            questionToSave.QuestionType = QuestionType;
            questionToSave.Answers = new List<Answer>
            {
                new Answer { Text = CorrectAnswer, IsCorrect = true }
            };

            if (QuestionType == QuestionType.MultipleChoice)
            {
                foreach (var option in Options)
                {
                    questionToSave.Answers.Add(new Answer { Text = option, IsCorrect = false });
                }
            }

            QuestionSavedRequested?.Invoke(questionToSave);
        }

        private bool CanGenerateQuestions(QuestionType type)
        {
            return TopicId > 0;
        }

        private async Task GenerateQuestionsAsync(QuestionType type)
        {
            try
            {
                bool success = await _questionService.GenerateAndSaveQuestionsAsync(TopicId, type);

                if (success)
                {
                    MessageBox.Show($"{type} típusú kérdések sikeresen generálva!", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Nem sikerült kérdéseket generálni.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a kérdésgenerálás során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}