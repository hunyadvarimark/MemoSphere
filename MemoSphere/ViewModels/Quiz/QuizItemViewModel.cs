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

        private string _evaluationFeedback;
        public string EvaluationFeedback
        {
            get => _evaluationFeedback;
            set => SetProperty(ref _evaluationFeedback, value);
        }
        public string SampleAnswer => Question.Answers?.FirstOrDefault(a => a.IsCorrect)?.SampleAnswer ?? string.Empty;
        public bool IsShortAnswer => Question.QuestionType == Core.Enums.QuestionType.ShortAnswer;

        public ObservableCollection<Answer> AnswerOptions { get; }

        public QuizItemViewModel(Question question)
        {
            Question = question ?? throw new ArgumentNullException(nameof(question));

            if (question.Answers != null && question.Answers.Any() && !IsShortAnswer)
            {
                var random = new Random();
                var shuffledAnswers = question.Answers.OrderBy(a => random.Next()).ToList();
                AnswerOptions = new ObservableCollection<Answer>(shuffledAnswers);
            }
            else
            {
                AnswerOptions = new ObservableCollection<Answer>();
            }
        }

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
                if (IsShortAnswer)
                {
                    if (string.IsNullOrWhiteSpace(UserAnswerText)) return false;
                    return _evalResult ?? false;
                }
                else
                {
                    return SelectedAnswer?.IsCorrect ?? false;
                }
            }
        }

        public void SetLLMEvaluationResult(bool isCorrect, string explanation)
        {
            _evalResult = isCorrect;
            EvaluationFeedback = explanation;
            OnPropertyChanged(nameof(IsCorrect));
        }

        public void Reset()
        {
            _selectedAnswer = null;
            _isAnswerSubmitted = false;
            _userAnswerText = string.Empty;
            _evalResult = null;

            OnPropertyChanged(nameof(SelectedAnswer));
            OnPropertyChanged(nameof(IsAnswerSubmitted));
            OnPropertyChanged(nameof(UserAnswerText));
            OnPropertyChanged(nameof(IsCorrect));
        }
    }
}