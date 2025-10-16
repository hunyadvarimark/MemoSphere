using Core.Entities;
using WPF.Utilities;

namespace WPF.ViewModels.Subjects
{
    public class SubjectViewModel : BaseViewModel
    {
        public Subject Subject { get; }

        public int Id => Subject.Id;
        public string Title
        {
            get => Subject.Title;
            set
            {
                Subject.Title = value;
                OnPropertyChanged();
            }
        }
        public SubjectViewModel(Subject subject)
        {
            Subject = subject;
        }
    }
}