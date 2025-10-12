using Core.Entities;
using Core.Interfaces.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using WPF.Utilities;

namespace WPF.ViewModels
{
    public class NoteDetailViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        public event Action<Note> NoteSavedSuccessfully;
        public event Action NoteDeletedSuccessfully;
        public AsyncCommand<object> DeleteNoteCommand { get; }
        public AsyncCommand<object> SaveNoteCommand { get; }

        private Note _currentNote;
        private string _noteTitle = string.Empty;
        public string NoteTitle
        {
            get => _noteTitle;
            set
            {
                if (SetProperty(ref _noteTitle, value))
                {
                    SaveNoteCommand.RaiseCanExecuteChanged();
                }
                Debug.WriteLine($"NoteTitle változott: '{_noteTitle}' – command requery");
            }
        }

        private string _noteContent = string.Empty;
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

        private int _selectedTopicId;
        public int SelectedTopicId
        {
            get => _selectedTopicId;
            set
            {
                Debug.WriteLine($"SelectedTopicId setter hívva: régi={_selectedTopicId}, új={value}");
                if (SetProperty(ref _selectedTopicId, value))
                {
                    SaveNoteCommand.RaiseCanExecuteChanged();
                }
                Debug.WriteLine("Command requery hívva SelectedTopicId változás után");
            }
        }

        private string _statusMessage = "Kész új jegyzet hozzáadására.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
                OnPropertyChanged();
                SaveNoteCommand.RaiseCanExecuteChanged();
                DeleteNoteCommand.RaiseCanExecuteChanged();
            }
        }
        public NoteDetailViewModel(INoteService noteService)
        {
            _noteService = noteService;
            SaveNoteCommand = new AsyncCommand<object>(
                    execute: SaveNoteAsync,
                    canExecute: CanSaveNote
            );
            DeleteNoteCommand = new AsyncCommand<object>(DeleteNoteAsync, CanDeleteNote);
        }
        private async Task SaveNoteAsync(object parameter)
        {
            IsSaving = true;

            if (string.IsNullOrWhiteSpace(NoteTitle) || string.IsNullOrWhiteSpace(NoteContent) || SelectedTopicId <= 0)
            {
                StatusMessage = "Hiba: A cím és a tartalom nem lehet üres, és egy témát ki kell választani.";
                IsSaving = false;
                return;
            }

            Note noteToSave;

            try
            {
                if (_currentNote != null)
                {
                    noteToSave = _currentNote;
                    noteToSave.Title = NoteTitle;
                    noteToSave.Content = NoteContent;
                    noteToSave.TopicId = SelectedTopicId;

                    await _noteService.UpdateNoteAsync(noteToSave);
                    StatusMessage = "A jegyzet sikeresen frissítve.";
                }
                else
                {
                    noteToSave = new Note
                    {
                        Title = NoteTitle,
                        Content = NoteContent,
                        TopicId = SelectedTopicId
                    };
                    var addedNote = await _noteService.AddNoteAsync(noteToSave);

                    noteToSave.Id = addedNote;
                    StatusMessage = "Új jegyzet sikeresen hozzáadva.";
                }

                // Frissítés után maradunk a mentett jegyzeten (szerkeszthető marad)
                // Hozzáadás után átállunk a most mentett jegyzetre (így az is szerkeszthető lesz)
                _currentNote = noteToSave;

                NoteSavedSuccessfully?.Invoke(noteToSave);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hiba a jegyzet mentése során: {ex.Message}";
                Console.WriteLine($"Mentési hiba: {ex.ToString()}");
            }
            finally
            {
                IsSaving = false;
                SaveNoteCommand.RaiseCanExecuteChanged();
            }
        }
        public void SetCurrentNote(Note note)
        {
            _currentNote = note;
            if (note != null)
            {
                NoteTitle = note.Title;
                NoteContent = note.Content;
            }
            else
            {
                NoteTitle = string.Empty;
                NoteContent = string.Empty;
            }

            SaveNoteCommand.RaiseCanExecuteChanged();
        }
        private bool CanDeleteNote(object parameter)
        {
            return _currentNote != null && !IsSaving;
        }
        private async Task DeleteNoteAsync(object parameter)
        {
            if (_currentNote == null)
            {
                StatusMessage = "Nincs törölhető jegyzet kiválasztva.";
                return;
            }
            IsSaving = true;
            try
            {
                await _noteService.DeleteNoteAsync(_currentNote.Id);
                StatusMessage = "A jegyzet sikeresen törölve.";
                // Törlés után ürítjük a mezőket és a jelenlegi jegyzetet
                SetCurrentNote(null);
                NoteDeletedSuccessfully?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hiba a jegyzet törlése során: {ex.Message}";
                Console.WriteLine($"Törlési hiba: {ex.ToString()}");
            }
            finally
            {
                IsSaving = false;
                SaveNoteCommand.RaiseCanExecuteChanged();
                DeleteNoteCommand.RaiseCanExecuteChanged();
            }
        }
        private bool CanSaveNote(object parameter)
        {
            return !IsSaving
                && !string.IsNullOrWhiteSpace(NoteTitle)
                && !string.IsNullOrWhiteSpace(NoteContent)
                && SelectedTopicId > 0;
        }
    }
}