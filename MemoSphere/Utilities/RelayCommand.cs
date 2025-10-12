// Fájl: WPF.Utilities/RelayCommand.cs

using System;
using System.Windows.Input;

namespace WPF.Utilities
{
    // A RelayCommand egy általános célú ICommand implementáció, amely delegáltakat használ a logikához.
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        // A CanExecuteChanged esemény a CommandManager RequerySuggested eseményét használja.
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Konstruktor csak végrehajtási akcióval (CanExecute mindig true)
        public RelayCommand(Action<object> execute) : this(execute, null) { }

        // Konstruktor mindkét akcióval
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            // Ha a canExecute delegált null, akkor a parancs mindig végrehajtható.
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            // Végrehajtja a parancsot, ha az végrehajtható
            if (CanExecute(parameter))
            {
                _execute(parameter);
            }
        }
    }
}