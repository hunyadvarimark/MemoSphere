using Core.Entities;
using Core.Interfaces.Services;
using System.Diagnostics;

namespace WPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {

        public HierarchyViewModel HierarchyVM { get; }
        public NoteDetailViewModel AddNoteVM { get; }
        public SubjectDetailViewModel SubjectDetailVM { get; }
        public TopicDetailViewModel TopicDetailVM { get; }
        private Note _noteBeingSaved;

        public MainViewModel(
            ISubjectService subjectService,
            ITopicService topicService,
            INoteService noteService,
            SubjectDetailViewModel subjectDetailVM,
            TopicDetailViewModel topicDetailViewModel)
        {

            // Inicializáljuk a két al-ViewModelt, átadva nekik a szükséges Service-eket
            HierarchyVM = new HierarchyViewModel(subjectService, topicService, noteService, subjectDetailVM, topicDetailViewModel);
            AddNoteVM = new NoteDetailViewModel(noteService);

            HierarchyVM.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(HierarchyVM.SelectedTopic))
                {
                    // 1. Átadjuk a kiválasztott Topic ID-t az AddNoteVM-nek
                    AddNoteVM.SelectedTopicId = HierarchyVM.SelectedTopic?.Id ?? 0;

                    AddNoteVM.SetCurrentNote(null);
                }
                if (e.PropertyName == nameof(HierarchyVM.SelectedNote))
                {
                    // 2. A kiválasztott jegyzetet betöltjük az AddNoteVM-be szerkesztésre
                    AddNoteVM.SetCurrentNote(HierarchyVM.SelectedNote);
                }
            };

            AddNoteVM.NoteSavedSuccessfully += Note => AddNoteVM_NoteSavedSuccessfully(Note);
            AddNoteVM.NoteDeletedSuccessfully += AddNoteVM_NoteDeletedSuccessfully;
        }
        private async void AddNoteVM_NoteSavedSuccessfully(Note savedNote)
        {
            // Ha van kiválasztott Topic, frissíteni kell a Notes listát.
            // Ezt a HierarchyVM.LoadNotesAsync() metódus futtatásával tehetjük meg.
            // Mivel a LoadNotesAsync egy private metódus, csináljunk egy public metódust rá a HierarchyVM-ben.

            if (HierarchyVM.SelectedTopic != null)
            {
                // 1. NEM TÖLTJÜK ÚJRA AZ EGÉSZ LISTÁT!
                // Helyette hívjuk a helyi frissítést:
                HierarchyVM.UpdateNoteInList(savedNote);

                // 2. Kiválasztás Visszaállítása
                // Ez kritikus, mert az UpdateNoteInList felcserélte az objektumot.
                // Beállítjuk a kiválasztott jegyzetet az újonnan mentett objektumra.
                HierarchyVM.SelectedNote = savedNote;

                // Ezzel a szerkesztő azonnal a frissített objektumra mutat.
            }
        }
        private async void AddNoteVM_NoteDeletedSuccessfully()
        {
            await HierarchyVM.ReloadNotesAsync();

            HierarchyVM.SelectedNote = null;
        }
    }
}
