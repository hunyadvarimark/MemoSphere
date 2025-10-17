using Core.Entities;
using WPF.ViewModels;
using System.Collections.ObjectModel;

namespace WPF.ViewModels.Quiz
{
    public class QuizItemViewModel : BaseViewModel
    {
        public Question Question { get; }

        private Answer _selectedAnswer;
        private bool _isAnswerSubmitted;
        private string _userAnswerText;
        private bool? _evalResult;
        public string SampleAnswer => Question.Answers?.FirstOrDefault(a => a.IsCorrect)?.SampleAnswer ?? string.Empty;
        public bool IsShortAnswer => Question.QuestionType == Core.Enums.QuestionType.ShortAnswer;

        public ObservableCollection<Answer> AnswerOptions { get; }

        public QuizItemViewModel(Question question)
        {
            Question = question ?? throw new ArgumentNullException(nameof(question));

            if (question.Answers == null && !IsShortAnswer || !question.Answers.Any() && !IsShortAnswer)
            {
                AnswerOptions = new ObservableCollection<Answer>();
            }
            else
            {
                var random = new Random();
                var shuffledAnswers = question.Answers.OrderBy(a => random.Next()).ToList();
                AnswerOptions = new ObservableCollection<Answer>(shuffledAnswers);
            }
        }

        // Tulajdonságok a Binding-hez
        public Answer SelectedAnswer
        {
            get => _selectedAnswer;
            set => SetProperty(ref _selectedAnswer, value);
        }
        public string UserAnswerText
        {
            get => _userAnswerText;
            set => SetProperty(ref _userAnswerText, value);
        }

        public bool IsAnswerSubmitted
        {
            get => _isAnswerSubmitted;
            set
            {
                if (SetProperty(ref _isAnswerSubmitted, value))
                {
                    OnPropertyChanged(nameof(IsCorrect));
                }
            }
        }

        public bool IsCorrect
        {
            get
            {
                if (!IsAnswerSubmitted)
                    return false;

                // --- RÖVID VÁLASZ LOGIKA ---
                if (IsShortAnswer)
                {
                    // 1. Üres válasz kizárása
                    if (string.IsNullOrWhiteSpace(UserAnswerText)) return false;

                    // 2. A kiértékelés eredményének visszaadása
                    // Csak akkor ad true-t, ha az LLM már kiértékelte, és az eredmény true.
                    return _evalResult ?? false;
                }
                // --- FELELETVÁLASZTÓS LOGIKA ---
                else
                {
                    return SelectedAnswer?.IsCorrect ?? false;
                }
            }
        }

        public void SetLLMEvaluationResult(bool isCorrect)
        {
            _evalResult = isCorrect;
            OnPropertyChanged(nameof(IsCorrect));
        }
    }
}