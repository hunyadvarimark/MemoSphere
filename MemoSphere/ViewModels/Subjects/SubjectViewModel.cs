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
        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        public SubjectViewModel(Subject subject)
        {
            Subject = subject;
        }
    }
}