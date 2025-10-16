using Core.Entities;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;
using WPF.Utilities;

namespace WPF.ViewModels.Subjects
{
    public class SubjectListViewModel : BaseViewModel
    {
        private readonly ISubjectService _subjectService;
        public ObservableCollection<SubjectViewModel> Subjects { get; } = new();
        private SubjectViewModel _selectedSubject;

        public RelayCommand EditSubjectCommand { get; }
        public RelayCommand DeleteSubjectCommand { get; }

        public event Action<Subject> EditSubjectRequested;
        public event Action<int> DeleteSubjectRequested;
        public event Action<SubjectViewModel> SubjectSelected;

        public SubjectViewModel SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                _selectedSubject = value;
                OnPropertyChanged();
                SubjectSelected?.Invoke(value);
            }
        }

        public SubjectListViewModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
            EditSubjectCommand = new RelayCommand(
                param => { if (param is SubjectViewModel subjectVM) EditSubjectRequested?.Invoke(subjectVM.Subject); },
                param => param is SubjectViewModel);
            DeleteSubjectCommand = new RelayCommand(
                param => { if (param is SubjectViewModel subjectVM && MessageBox.Show($"Biztosan törölni szeretnéd '{subjectVM.Title}'-t?", "Törlés", MessageBoxButton.YesNo) == MessageBoxResult.Yes) DeleteSubjectRequested?.Invoke(subjectVM.Id); },
                param => param is SubjectViewModel);
        }

        public async Task LoadSubjectsAsync()
        {
            Subjects.Clear();
            var subjects = await _subjectService.GetAllSubjectsAsync();
            foreach (var s in subjects)
                Subjects.Add(new SubjectViewModel(s));
        }

        public void ClearSubjects()
        {
            Subjects.Clear();
        }

        public void RemoveSubject(int subjectId)
        {
            var subjectToRemove = Subjects.FirstOrDefault(s => s.Id == subjectId);
            if (subjectToRemove != null)
            {
                Subjects.Remove(subjectToRemove);
                if (SelectedSubject == subjectToRemove)
                    SelectedSubject = null;
            }
        }
    }
}