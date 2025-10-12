using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPF.ViewModels
{
    /// <summary>
    /// Az összes ViewModel alapvető osztálya, amely támogatja a PropertyChanged eseményt.
    /// </summary>

    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Kiváltja a PropertyChanged eseményt a View frissítésére.
        /// </summary>
        /// <param name="propertyName">A megváltozott tulajdonság neve (automata kitöltés).</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}