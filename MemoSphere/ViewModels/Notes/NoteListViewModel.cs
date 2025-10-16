using Core.Entities;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;
using WPF.Utilities;

namespace WPF.ViewModels.Notes
{
    public class NoteListViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;

        public ObservableCollection<NoteViewModel> Notes { get; } = new();

        private NoteViewModel _selectedNote;
        public NoteViewModel SelectedNote
        {
            get => _selectedNote;
            set
            {
                _selectedNote = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand DeleteNoteCommand { get; }

        public event Action<int> DeleteNoteRequested;

        public NoteListViewModel(INoteService noteService)
        {
            _noteService = noteService;

            DeleteNoteCommand = new RelayCommand(
                param =>
                {
                    if (param is NoteViewModel noteVM)
                    {
                        var result = MessageBox.Show(
                            $"Biztosan törölni szeretnéd a '{noteVM.Note.Title}' jegyzetet?",
                            "Törlés megerősítése",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            DeleteNoteRequested?.Invoke(noteVM.Note.Id);
                        }
                    }
                },
                param => param is NoteViewModel);
        }

        public async Task LoadNotesAsync(int topicId)
        {
            Notes.Clear();
            var notes = await _noteService.GetNotesByTopicIdAsync(topicId);
            foreach (var n in notes)
                Notes.Add(new NoteViewModel(n));
            SelectedNote = null;
        }

        public void ClearNotes()
        {
            Notes.Clear();
        }

        public void RemoveNoteFromList(int noteId)
        {
            var existing = Notes.FirstOrDefault(n => n.Id == noteId);
            if (existing != null)
                Notes.Remove(existing);
        }
    }
}