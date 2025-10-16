using Core.Entities;
using WPF.ViewModels;

namespace WPF.ViewModels.Quiz
{
    public class QuizItemViewModel : BaseViewModel
    {
        public Question Question { get; }

        private string _selectedAnswerText;
        private bool _isAnswerSubmitted;

        public List<string> AnswerOptions { get; }
        public QuizItemViewModel(Question question)
        {
            Question = question ?? throw new ArgumentNullException(nameof(question));

            if(question.Answers == null || !question.Answers.Any())
                AnswerOptions = new List<string>();
            else
            {
                var random = new Random();
                
                AnswerOptions = Question.Answers?
                    .OrderBy(a => random.Next())
                    .Select(a => a.Text)
                    .ToList() ?? new List<string>();
            }
                
        }

        // --- Tulajdonságok a Binding-hez ---

        public string SelectedAnswerText
        {
            get => _selectedAnswerText;
            set => SetProperty(ref _selectedAnswerText, value);
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
                if (!IsAnswerSubmitted || Question.Answers == null || string.IsNullOrEmpty(SelectedAnswerText))
                    return false;

                return Question.Answers
                               .Any(a => a.Text == SelectedAnswerText && a.IsCorrect);
            }
        }
    }
}