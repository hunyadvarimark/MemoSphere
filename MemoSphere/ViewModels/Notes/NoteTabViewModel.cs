using Core.Entities;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using WPF.Utilities;
using WPF.ViewModels.Questions;
using System.Linq;

namespace WPF.ViewModels.Notes
{
    public class NoteTabViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        private readonly QuestionListViewModel _questionListVM;
        private Note _note;
        private bool _isActive;
        private bool _hasUnsavedChanges;

        public Note Note
        {
            get => _note;
            set
            {
                if (SetProperty(ref _note, value))
                {
                    OnPropertyChanged(nameof(Title));
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        public string Title
        {
            get => _note?.Title ?? "Új jegyzet";
            set
            {
                if (_note != null && _note.Title != value)
                {
                    _note.Title = value;
                    OnPropertyChanged();
                    HasUnsavedChanges = true;
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string Content
        {
            get => _note?.Content ?? string.Empty;
            set
            {
                if (_note != null && _note.Content != value)
                {
                    _note.Content = value;
                    OnPropertyChanged();
                    HasUnsavedChanges = true;
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public bool HasQuestions => _questionListVM.Questions.Count > 0;

        public ObservableCollection<Question> Questions => _questionListVM.Questions;

        public IEnumerable<Question> DistinctQuestions => Questions.GroupBy(q => q.Text).Select(g => g.First());

        // Commands
        public AsyncCommand<object> SaveCommand { get; }
        public RelayCommand CloseCommand { get; }
        public AsyncCommand<object> GenerateQuestionsCommand { get; }
        public RelayCommand ActivateCommand { get; }

        // Events
        public event Action<NoteTabViewModel> CloseRequested;
        public event Action<Note> NoteSaved;
        public event Action<NoteTabViewModel> ActivateRequested;

        public NoteTabViewModel(
            Note note,
            INoteService noteService,
            QuestionListViewModel questionListVM)
        {
            _note = note ?? throw new ArgumentNullException(nameof(note));
            _noteService = noteService ?? throw new ArgumentNullException(nameof(noteService));
            _questionListVM = questionListVM ?? throw new ArgumentNullException(nameof(questionListVM));

            SaveCommand = new AsyncCommand<object>(SaveNoteAsync, CanSave);
            CloseCommand = new RelayCommand(_ => RequestClose());
            GenerateQuestionsCommand = _questionListVM.GenerateQuestionsCommand;
            ActivateCommand = new RelayCommand(_ => ActivateRequested?.Invoke(this));

            if (note.Id > 0)
            {
                _ = LoadQuestionsAsync();
            }

            _questionListVM.Questions.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasQuestions));
                OnPropertyChanged(nameof(DistinctQuestions));
            };
        }

        private async Task LoadQuestionsAsync()
        {
            try
            {
                await _questionListVM.LoadQuestionsAsync(Note.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hiba a kérdések betöltésekor: {ex.Message}");
            }
        }

        private bool CanSave(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Note?.Title)
                && !string.IsNullOrWhiteSpace(Note?.Content)
                && Note?.TopicId > 0;
        }

        private async Task SaveNoteAsync(object parameter)
        {
            if (!CanSave(null)) return;

            try
            {
                NoteSaved?.Invoke(Note);
                HasUnsavedChanges = false;

                if (Note.Id > 0)
                {
                    await LoadQuestionsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Hiba a mentés során: {ex.Message}",
                    "Hiba",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void RequestClose()
        {
            if (HasUnsavedChanges)
            {
                var result = System.Windows.MessageBox.Show(
                    $"A '{Title}' jegyzetnek vannak mentetlen változásai. Biztosan bezárod?",
                    "Mentetlen változások",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.No)
                    return;
            }

            CloseRequested?.Invoke(this);
        }

        public void MarkAsSaved()
        {
            HasUnsavedChanges = false;
        }

        public async Task RefreshQuestionsAsync()
        {
            if (Note?.Id > 0)
            {
                await LoadQuestionsAsync();
            }
        }
    }
}