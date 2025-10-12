using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF.Utilities;
public class AsyncCommand<T> : ICommand
{
    private readonly Func<T, Task> _execute;
    private readonly Predicate<T> _canExecute;
    private bool _isExecuting;

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public AsyncCommand(Func<T, Task> execute, Predicate<T> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        if (_isExecuting) return false;

        if (_canExecute == null) return true;

        if (parameter is T tParameter)
        {
            bool can = _canExecute(tParameter);
            //Debug.WriteLine($"CanExecute ellenőrzés: Result={can}, IsSaving={_isSaving}, Title üres?={string.IsNullOrWhiteSpace(NoteTitle)}, Content üres?={string.IsNullOrWhiteSpace(NoteContent)}, SelectedTopicId={SelectedTopicId}");  // Részletes log
            return can;
        }
        return typeof(T) == typeof(object) ? _canExecute((T)parameter) : false;
    }

    public async void Execute(object parameter)
    {
        await ExecuteAsync(parameter);
    }
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
    public async Task ExecuteAsync(object parameter)
    {
        if (CanExecute(parameter))
        {
            _isExecuting = true;
            try
            {
                CommandManager.InvalidateRequerySuggested();

                await _execute((T)parameter);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}