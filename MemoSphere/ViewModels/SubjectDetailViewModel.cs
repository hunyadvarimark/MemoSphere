using Core.Entities;
using Core.Interfaces.Services;
using System.Windows.Input;
using WPF.Utilities;

namespace WPF.ViewModels
{
    public class SubjectDetailViewModel : BaseViewModel
    {
        private readonly ISubjectService _subjectService;
        private string _subjectTitle;
        private Subject _currentSubject;

        public string SubjectTitle
        {
            get => _subjectTitle;
            set
            {
                if (SetProperty(ref _subjectTitle, value))
                {
                    SaveSubjectCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public AsyncCommand<object> SaveSubjectCommand { get; }

        public event Action<Subject> SubjectSavedSuccessfully;
        public ICommand CancelCommand { get; }
        public event Action CancelRequested;

        public void RaiseCanExecuteChanged()
        {
            SaveSubjectCommand.RaiseCanExecuteChanged();
        }

        public SubjectDetailViewModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
            SaveSubjectCommand = new AsyncCommand<object>(SaveSubjectAsync, CanSaveSubject);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        public void LoadSubject(Subject subjectToEdit)
        {
            _currentSubject = subjectToEdit;
            SubjectTitle = subjectToEdit.Title;
            RaiseCanExecuteChanged();
        }
        private bool CanSaveSubject(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SubjectTitle);
        }

        private async Task SaveSubjectAsync(object parameter)
        {
            if (string.IsNullOrWhiteSpace(SubjectTitle)) return;

            try
            {
                Subject resultSubject;
                string trimmedTitle = SubjectTitle.Trim();

                int? excludeId = _currentSubject?.Id;

                if (await _subjectService.SubjectExistsAsync(trimmedTitle, excludeId))
                {
                    Console.WriteLine("Hiba: A megadott tantárgy címe már létezik.");
                    return;
                }

                if (_currentSubject != null)
                {
                    _currentSubject.Title = trimmedTitle;

                    await _subjectService.UpdateSubjectAsync(_currentSubject);
                    resultSubject = _currentSubject;
                }
                else
                {
                    resultSubject = await _subjectService.AddSubjectAsync(trimmedTitle);
                }
                SubjectTitle = string.Empty;

                SubjectSavedSuccessfully?.Invoke(resultSubject);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Adatbeviteli hiba: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a tantárgy mentése során: {ex.Message}");
            }
        }
        public void ResetState()
        {
            SubjectTitle = string.Empty;
            _currentSubject = null;
            RaiseCanExecuteChanged();
        }
        private void ExecuteCancel(object parameter)
        {
            // Ez csak jelzi a HierarchyViewModel-nek, hogy be kell zárni az ablakot.
            CancelRequested?.Invoke();
            ResetState();
        }
    }
}
