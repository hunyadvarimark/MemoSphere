using System.Windows.Controls;
using System.Windows;
using Core.Entities;
using WPF.ViewModels;

namespace WPF.Views
{
    public partial class HierarchyView : UserControl
    {
        public HierarchyView()
        {
            InitializeComponent();
        }

        // 🏆 Ez a metódus reagál a TreeView-ben történő kiválasztásra
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Ellenőrizzük, hogy a DataContext a HierarchyViewModel-e
            if (DataContext is HierarchyViewModel viewModel)
            {
                // A e.NewValue tartalmazza a kiválasztott entitást (Subject vagy Topic)

                if (e.NewValue is Subject selectedSubject)
                {
                    // Ha Subject-et választottunk ki
                    viewModel.SelectedSubject = selectedSubject;
                    // A ViewModel logikája gondoskodik a Topics és Notes frissítéséről
                }
                else if (e.NewValue is Topic selectedTopic)
                {
                    // Ha Topic-ot választottunk ki
                    viewModel.SelectedTopic = selectedTopic;
                    // Meg kell győződnünk róla, hogy a szülő Subject is be legyen állítva, ha nem volt beállítva (bár a láncolt betöltés miatt ez általában már megtörténik)
                    // Itt nem kell a SelectedSubject-et beállítanunk, mert az már a Topics betöltésekor megtörtént.
                }

                // Mivel a HierarchyViewModel-ben nincsenek közvetlen SelectedNote beállítások ehhez a TreeView-hoz,
                // a SelectedNote-ot a LoadNotesAsync metódus fogja beállítani a SelectedTopic változásakor.
            }
        }
    }
}