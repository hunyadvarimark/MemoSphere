using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;
using Data.Services;
using WPF.Utilities;

namespace WPF.ViewModels.Notes
{
    public class NoteDetailViewModel : BaseViewModel
    {
        private Note _currentNote;
        private string _noteTitle = string.Empty;
        private string _noteContent = string.Empty;
        private int _selectedTopicId;

        public string NoteTitle
        {
            get => _noteTitle;
            set
            {
                if (SetProperty(ref _noteTitle, value))
                {
                    SaveNoteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string NoteContent
        {
            get => _noteContent;
            set
            {
                if (SetProperty(ref _noteContent, value))
                {
                    SaveNoteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public int SelectedTopicId
        {
            get => _selectedTopicId;
            set
            {
                if (SetProperty(ref _selectedTopicId, value))
                {
                    SaveNoteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public AsyncCommand<object> SaveNoteCommand { get; }
        public AsyncCommand<object> DeleteNoteCommand { get; }

        public event Action<Note> NoteSavedRequested;
        public event Action<int> NoteDeleteRequested;

        public NoteDetailViewModel(IQuestionService questionService)
        {
            SaveNoteCommand = new AsyncCommand<object>(SaveNoteRequestedAsync, CanSaveNote);
            DeleteNoteCommand = new AsyncCommand<object>(DeleteNoteAsync, CanDeleteNote);
        }

        /// <summary>
        /// Betölti a jegyzetet szerkesztésre, vagy reseteli új jegyzet hozzáadásához.
        /// Ha note == null, csak a Title és Content mezőket törli, a SelectedTopicId-t MEGTARTJA!
        /// </summary>
        public void SetCurrentNote(Note note)
        {
            _currentNote = note;
            NoteTitle = note?.Title ?? string.Empty;
            NoteContent = note?.Content ?? string.Empty;

            // KRITIKUS: Csak akkor állítjuk be a TopicId-t, ha van note
            if (note != null)
            {
                SelectedTopicId = note.TopicId;
            }
            // Ha note == null, akkor NEM írjuk felül a már beállított SelectedTopicId-t!

            SaveNoteCommand.RaiseCanExecuteChanged();
            DeleteNoteCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Teljesen reseteli a viewmodelt (pl. topic váltásnál)
        /// </summary>
        public void ResetState()
        {
            _currentNote = null;
            NoteTitle = string.Empty;
            NoteContent = string.Empty;
            SelectedTopicId = 0;
            SaveNoteCommand.RaiseCanExecuteChanged();
            DeleteNoteCommand.RaiseCanExecuteChanged();
        }

        private bool CanSaveNote(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NoteTitle)
                && !string.IsNullOrWhiteSpace(NoteContent)
                && SelectedTopicId > 0;
        }

        private async Task SaveNoteRequestedAsync(object parameter)
        {
            if (!CanSaveNote(null)) return;

            Note noteToSave = _currentNote ?? new Note { TopicId = SelectedTopicId };
            noteToSave.Title = NoteTitle;
            noteToSave.Content = NoteContent;
            noteToSave.TopicId = SelectedTopicId;

            NoteSavedRequested?.Invoke(noteToSave);
        }

        private bool CanDeleteNote(object parameter)
        {
            return _currentNote != null;
        }

        private async Task DeleteNoteAsync(object parameter)
        {
            if (_currentNote == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Biztosan törölni szeretnéd a '{_currentNote.Title}' jegyzetet?",
                "Törlés megerősítése",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                NoteDeleteRequested?.Invoke(_currentNote.Id);
                ResetState();
            }
        }

        private bool CanGenerateQuestions(QuestionType type)
        {
            return _currentNote != null && _currentNote.Id > 0;
        }
    }
}