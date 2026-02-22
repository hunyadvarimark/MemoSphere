using Core.Entities;
using System.Windows.Input;
using WPF.Utilities;
using WPF.ViewModels;

public class SubjectDetailViewModel : BaseViewModel
{
    private Subject _currentSubject;
    private string _subjectTitle = string.Empty;

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
    public ICommand CancelCommand { get; }

    public event Action<Subject> SubjectSavedRequested;
    public event Action CancelRequested;

    public SubjectDetailViewModel()
    {
        SaveSubjectCommand = new AsyncCommand<object>(SaveSubjectRequestedAsync, CanSaveSubject);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    public void ResetState()
    {
        _currentSubject = null;
        SubjectTitle = string.Empty;
        SaveSubjectCommand.RaiseCanExecuteChanged();
    }

    public void LoadSubject(Subject subjectToEdit)
    {
        _currentSubject = subjectToEdit;
        SubjectTitle = subjectToEdit?.Title ?? string.Empty;
        SaveSubjectCommand.RaiseCanExecuteChanged();
    }

    private bool CanSaveSubject(object parameter)
    {
        return !string.IsNullOrWhiteSpace(SubjectTitle);
    }

    private Task SaveSubjectRequestedAsync(object parameter)
    {
        if (!CanSaveSubject(null))
            return Task.CompletedTask;

        string trimmedTitle = SubjectTitle.Trim();
        Subject subjectToSave = _currentSubject ?? new Subject();
        subjectToSave.Title = trimmedTitle;

        SubjectSavedRequested?.Invoke(subjectToSave);

        return Task.CompletedTask;
    }

    private void ExecuteCancel(object parameter)
    {
        CancelRequested?.Invoke();
        ResetState();
    }
}