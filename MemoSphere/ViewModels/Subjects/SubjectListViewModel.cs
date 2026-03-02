using Core.Entities;
using Core.Interfaces.Services;
using MemoSphere.Data.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using WPF.Utilities;

namespace WPF.ViewModels.Subjects
{
    public class SubjectListViewModel : BaseViewModel
    {
        private readonly ISubjectService _subjectService;
        private readonly INoteShareService _noteShareService;
        public ObservableCollection<SubjectViewModel> Subjects { get; } = new();
        private SubjectViewModel _selectedSubject;

        

        public RelayCommand EditSubjectCommand { get; }
        public RelayCommand DeleteSubjectCommand { get; }
        public RelayCommand SelectSubjectCommand { get; }
        public AsyncCommand<object> ExportSubjectCommand { get; }

        public event Action<Subject> EditSubjectRequested;
        public event Action<int> DeleteSubjectRequested;
        public event Action<SubjectViewModel> SubjectSelected;


        public SubjectViewModel SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (_selectedSubject != value)
                {
                    if (_selectedSubject != null)
                        _selectedSubject.IsSelected = false;

                    _selectedSubject = value;

                    if (_selectedSubject != null)
                        _selectedSubject.IsSelected = true;

                    OnPropertyChanged();
                    SubjectSelected?.Invoke(value);
                }
            }
        }

        public SubjectListViewModel(ISubjectService subjectService, INoteShareService noteShareService)
        {
            _subjectService = subjectService;
            _noteShareService = noteShareService;
            EditSubjectCommand = new RelayCommand(
                param => { if (param is SubjectViewModel subjectVM) EditSubjectRequested?.Invoke(subjectVM.Subject); },
                param => param is SubjectViewModel);
            DeleteSubjectCommand = new RelayCommand(
                param => { if (param is SubjectViewModel subjectVM && MessageBox.Show($"Biztosan törölni szeretnéd '{subjectVM.Title}'-t?", "Törlés", MessageBoxButton.YesNo) == MessageBoxResult.Yes) DeleteSubjectRequested?.Invoke(subjectVM.Id); },
                param => param is SubjectViewModel);
            SelectSubjectCommand = new RelayCommand(
                param =>
                {
                    if (param is SubjectViewModel vm)
                    {
                        SelectedSubject = vm;
                    }
                }
            );
            ExportSubjectCommand = new AsyncCommand<object>(async param => {
                if (param is SubjectViewModel svm)
                {
                    var sfd = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "MemoSphere fájl (*.memo)|*.memo",
                        FileName = $"{svm.Title}.memo"
                    };

                    if (sfd.ShowDialog() == true)
                    {
                        try
                        {
                            await _noteShareService.ExportSubjectToFileAsync(svm.Id, sfd.FileName);
                            System.Windows.MessageBox.Show("Tantárgy sikeresen exportálva!", "Siker");
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Hiba: {ex.Message}", "Hiba");
                        }
                    }
                }
            });

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