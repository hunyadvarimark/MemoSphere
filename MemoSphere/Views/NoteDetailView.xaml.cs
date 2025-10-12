using System.Diagnostics;
using System.Windows.Controls;

namespace WPF.Views
{
    /// <summary>
    /// Interaction logic for AddNoteView.xaml
    /// </summary>
    public partial class NoteDetailView : UserControl
    {
        public NoteDetailView()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                Debug.WriteLine($"🟢 NoteDetailView DataContext: {DataContext?.GetType().Name ?? "null"}");
            };
        }
    }
}
