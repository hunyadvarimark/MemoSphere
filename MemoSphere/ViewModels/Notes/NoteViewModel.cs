using Core.Entities;
using WPF.Utilities;

namespace WPF.ViewModels.Notes
{
    public class NoteViewModel : BaseViewModel
    {
        public Note Note { get; }

        public int Id => Note.Id;
        public string Title
        {
            get => Note.Title;
            set
            {
                Note.Title = value;
                OnPropertyChanged();
            }
        }
        public string Content
        {
            get => Note.Content;
            set
            {
                Note.Content = value;
                OnPropertyChanged();
            }
        }

        public NoteViewModel(Note note)
        {
            Note = note;
        }
    }
}