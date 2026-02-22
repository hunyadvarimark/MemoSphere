using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPF.Utilities;

namespace WPF.ViewModels.Questions
{
    public class QuestionDetailViewModel : BaseViewModel
    {
        public class OptionWrapper : BaseViewModel
        {
            private string _text = string.Empty;
            public string Text
            {
                get => _text;
                set => SetProperty(ref _text, value);
            }
        }

        private readonly IQuestionService _questionService;
        private Question? _currentQuestion;
        private string _questionText = string.Empty;
        private QuestionType _questionType;
        private string _correctAnswer = string.Empty;
        private int _topicId;
        private int? _noteId;
        private bool _isNewQuestion;
        private ObservableCollection<OptionWrapper> _options = new();

        #region Properties

        public string QuestionText
        {
            get => _questionText;
            set { if (SetProperty(ref _questionText, value)) SaveQuestionCommand.RaiseCanExecuteChanged(); }
        }

        public QuestionType QuestionType
        {
            get => _questionType;
            set { if (SetProperty(ref _questionType, value)) SaveQuestionCommand.RaiseCanExecuteChanged(); }
        }

        public string CorrectAnswer
        {
            get => _correctAnswer;
            set { if (SetProperty(ref _correctAnswer, value)) SaveQuestionCommand.RaiseCanExecuteChanged(); }
        }

        public ObservableCollection<OptionWrapper> Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

        public int TopicId
        {
            get => _topicId;
            set { if (SetProperty(ref _topicId, value)) SaveQuestionCommand.RaiseCanExecuteChanged(); }
        }

        public int? NoteId
        {
            get => _noteId;
            set => SetProperty(ref _noteId, value);
        }

        public bool IsNewQuestion
        {
            get => _isNewQuestion;
            set => SetProperty(ref _isNewQuestion, value);
        }

        #endregion

        public AsyncCommand<object> SaveQuestionCommand { get; }
        public AsyncCommand<QuestionType> GenerateQuestionsCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action? CancelRequested;
        public event Action<Question>? QuestionSavedRequested;

        public QuestionDetailViewModel(IQuestionService questionService)
        {
            _questionService = questionService;
            SaveQuestionCommand = new AsyncCommand<object>(SaveQuestionRequestedAsync, CanSaveQuestion);
            GenerateQuestionsCommand = new AsyncCommand<QuestionType>(GenerateQuestionsAsync, CanGenerateQuestions);
            CancelCommand = new RelayCommand(_ => CancelRequested?.Invoke());
        }

        // ÚJ: Két paramétert vár, hogy a kérdés mindkét szülőhöz kötődjön
        public void ResetState(int topicId, int noteId)
        {
            _currentQuestion = null;
            IsNewQuestion = true;
            QuestionText = string.Empty;
            QuestionType = QuestionType.MultipleChoice;
            CorrectAnswer = string.Empty;
            Options.Clear();

            // Alapból adjunk hozzá 3 üres opciót a kényelmesebb bevitelhez
            Options.Add(new OptionWrapper());
            Options.Add(new OptionWrapper());
            Options.Add(new OptionWrapper());

            TopicId = topicId;
            NoteId = noteId;

            SaveQuestionCommand.RaiseCanExecuteChanged();
        }

        public void LoadQuestion(Question questionToEdit)
        {
            _currentQuestion = questionToEdit;
            IsNewQuestion = false;
            QuestionText = questionToEdit?.Text ?? string.Empty;
            QuestionType = questionToEdit?.QuestionType ?? QuestionType.MultipleChoice;
            CorrectAnswer = questionToEdit?.Answers?.FirstOrDefault(a => a.IsCorrect)?.Text ?? string.Empty;
            TopicId = questionToEdit?.TopicId ?? 0;
            NoteId = questionToEdit?.SourceNoteId; // Betöltjük a NoteId-t is

            Options.Clear();
            if (questionToEdit?.Answers != null)
            {
                foreach (var answer in questionToEdit.Answers.Where(a => !a.IsCorrect))
                {
                    Options.Add(new OptionWrapper { Text = answer.Text });
                }
            }

            SaveQuestionCommand.RaiseCanExecuteChanged();
        }

        private bool CanSaveQuestion(object parameter)
        {
            bool basicValid = !string.IsNullOrWhiteSpace(QuestionText)
                && !string.IsNullOrWhiteSpace(CorrectAnswer)
                && TopicId > 0;

            if (QuestionType == QuestionType.MultipleChoice)
            {
                return basicValid && Options.Count(o => !string.IsNullOrWhiteSpace(o.Text)) >= 1;
            }

            return basicValid;
        }

        private async Task SaveQuestionRequestedAsync(object parameter)
        {
            if (!CanSaveQuestion(null)) return;

            Question questionToSave = _currentQuestion ?? new Question();

            questionToSave.Text = QuestionText;
            questionToSave.QuestionType = QuestionType;
            questionToSave.TopicId = TopicId;
            questionToSave.SourceNoteId = NoteId;

            var answers = new List<Answer>
            {
                new Answer { Text = CorrectAnswer, IsCorrect = true, QuestionId = questionToSave.Id }
            };

            if (QuestionType == QuestionType.MultipleChoice)
            {
                foreach (var optionWrapper in Options)
                {
                    if (!string.IsNullOrWhiteSpace(optionWrapper.Text))
                    {
                        answers.Add(new Answer
                        {
                            Text = optionWrapper.Text,
                            IsCorrect = false,
                            QuestionId = questionToSave.Id
                        });
                    }
                }
            }

            questionToSave.Answers = answers;
            QuestionSavedRequested?.Invoke(questionToSave);
        }

        private bool CanGenerateQuestions(QuestionType type) => TopicId > 0;

        private async Task GenerateQuestionsAsync(QuestionType type)
        {
            try
            {
                bool success = await _questionService.GenerateAndSaveQuestionsAsync(TopicId, type);
                if (success)
                {
                    MessageBox.Show($"{type} típusú kérdések sikeresen generálva!", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Itt érdemes lenne egy eseményt dobni, hogy a UI frissüljön
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a kérdésgenerálás során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}