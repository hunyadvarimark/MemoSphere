using System.Windows;
using WPF.ViewModels.Quiz;

namespace WPF.Views.Quiz
{
    public partial class QuizWindow : Window
    {
        public QuizWindow(QuizViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Bezárás amikor a kvíz befejeződik
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(viewModel.IsQuizFinished) && viewModel.IsQuizFinished)
                {
                }
            };
        }
    }
}