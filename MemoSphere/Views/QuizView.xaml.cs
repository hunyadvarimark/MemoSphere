using System.Windows;
using System.Windows.Controls;
using WPF.ViewModels.Quiz;

namespace WPF.Views
{
    /// <summary>
    /// Interaction logic for QuizView.xaml
    /// </summary>
    public partial class QuizView : UserControl
    {
        public QuizView()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Bezárja a felugró ablakot
            Window.GetWindow(this)?.Close();
        }
    }
}
