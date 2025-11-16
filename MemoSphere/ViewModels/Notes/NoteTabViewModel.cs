using Core.Entities;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using WPF.Utilities;
using WPF.ViewModels.Questions;
using System.Linq;
using System.IO;

namespace WPF.ViewModels.Notes
{
    public class NoteTabViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        private readonly IDocumentImportService _documentImportService;
        private readonly QuestionListViewModel _questionListVM;
        private readonly MainViewModel _mainViewModel;

        private Note _note;
        private bool _isActive;
        private bool _hasUnsavedChanges;
        private string _originalContent; // ✅ Eredeti tartalom nyilvántartása

        public Note Note
        {
            get => _note;
            set
            {
                if (SetProperty(ref _note, value))
                {
                    OnPropertyChanged(nameof(Title));
                    OnPropertyChanged(nameof(Content));
                    // ✅ Eredeti tartalom tárolása betöltéskor
                    _originalContent = value?.Content ?? string.Empty;
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

                    if (!_isInEditMode)
                    {
                        IsMarkdownContent = DetectMarkdownContent(value);
                    }
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

        public bool IsGenerating => _questionListVM.IsGenerating;

        public string GenerationStatus => _questionListVM.IsGenerating
            ? "🤖 AI kérdések generálása folyamatban..."
            : string.Empty;

        public bool HasQuestions => _questionListVM.Questions.Count > 0;

        private bool _isImporting;
        public bool IsImporting
        {
            get => _isImporting;
            set => SetProperty(ref _isImporting, value);
        }

        private string _importStatus = string.Empty;
        public string ImportStatus
        {
            get => _importStatus;
            set => SetProperty(ref _importStatus, value);
        }

        private bool _isMarkdownContent = false;
        public bool IsMarkdownContent
        {
            get => _isMarkdownContent;
            set
            {
                if (SetProperty(ref _isMarkdownContent, value))
                {
                    if (value)
                    {
                        _isInEditMode = false;
                    }
                    else
                    {
                        _isInEditMode = true;
                    }
                }
            }
        }

        private bool _isInEditMode = false;

        private string _notificationMessage;
        public string NotificationMessage
        {
            get => _notificationMessage;
            set
            {
                if (SetProperty(ref _notificationMessage, value))
                {
                    OnPropertyChanged(nameof(HasNotification));
                }
            }
        }

        private string _notificationType = "Info";
        public string NotificationType
        {
            get => _notificationType;
            set => SetProperty(ref _notificationType, value);
        }

        public bool HasNotification => !string.IsNullOrEmpty(NotificationMessage);

        public ObservableCollection<Question> Questions => _questionListVM.Questions;

        public IEnumerable<Question> DistinctQuestions => Questions.GroupBy(q => q.Text).Select(g => g.First());

        // Commands
        public AsyncCommand<object> SaveCommand { get; }
        public RelayCommand CloseCommand { get; }
        public AsyncCommand<object> GenerateQuestionsCommand { get; }
        public RelayCommand ActivateCommand { get; }
        public RelayCommand ImportPdfCommand { get; }
        public RelayCommand ToggleEditModeCommand { get; }
        public RelayCommand CloseNotificationCommand { get; }
        public RelayCommand StartNoteQuizCommand { get; }

        // Events
        public event Action<NoteTabViewModel> CloseRequested;
        public event Action<Note> NoteSaved;
        public event Action<NoteTabViewModel> ActivateRequested;
        public event Action<int> NoteQuizRequested;

        public NoteTabViewModel(
            Note note,
            INoteService noteService,
            QuestionListViewModel questionListVM,
            IDocumentImportService documentImportService,
            MainViewModel mainViewModel)
        {
            _note = note ?? throw new ArgumentNullException(nameof(note));
            _noteService = noteService ?? throw new ArgumentNullException(nameof(noteService));
            _questionListVM = questionListVM ?? throw new ArgumentNullException(nameof(questionListVM));
            _documentImportService = documentImportService ?? throw new ArgumentNullException(nameof(documentImportService));
            _mainViewModel = mainViewModel;
            // ✅ Eredeti tartalom mentése inicializáláskor
            _originalContent = note?.Content ?? string.Empty;

            SaveCommand = new AsyncCommand<object>(SaveNoteAsync, CanSave);
            CloseCommand = new RelayCommand(_ => RequestClose());
            GenerateQuestionsCommand = _questionListVM.GenerateQuestionsCommand;
            ActivateCommand = new RelayCommand(_ => ActivateRequested?.Invoke(this));
            ImportPdfCommand = new RelayCommand(_ => ImportPdfAsync());
            ToggleEditModeCommand = new RelayCommand(_ => ToggleEditMode());
            CloseNotificationCommand = new RelayCommand(_ => NotificationMessage = null);
            StartNoteQuizCommand = new RelayCommand(
                _ => NoteQuizRequested?.Invoke(Note.Id),
                _ => Note.Id > 0 && HasQuestions
            );

            _questionListVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(QuestionListViewModel.IsGenerating))
                {
                    OnPropertyChanged(nameof(IsGenerating));
                    OnPropertyChanged(nameof(GenerationStatus));
                    GenerateQuestionsCommand?.RaiseCanExecuteChanged();
                }
            };

            if (note.Id > 0)
            {
                _ = LoadQuestionsAsync();
            }

            _questionListVM.Questions.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasQuestions));
                OnPropertyChanged(nameof(DistinctQuestions));
                
                StartNoteQuizCommand.RaiseCanExecuteChanged();
            };

            bool hasMarkdown = DetectMarkdownContent(note?.Content);
            IsMarkdownContent = hasMarkdown;
            _isInEditMode = !hasMarkdown;
        }

        private async void ShowNotification(string type, string message, int durationMs = 3000)
        {
            NotificationType = type;
            NotificationMessage = message;

            await Task.Delay(durationMs);
            if (NotificationMessage == message)
            {
                NotificationMessage = null;
            }
        }

        private void ToggleEditMode()
        {
            IsMarkdownContent = !IsMarkdownContent;

            if (!IsMarkdownContent && DetectMarkdownContent(Content))
            {
                var result = System.Windows.MessageBox.Show(
                    "Szerkesztési módba váltasz. A Markdown/LaTeX formázás csak előnézeti módban látható.\n\n" +
                    "Folytatod?",
                    "Szerkesztési mód",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);

                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    IsMarkdownContent = true;
                    return;
                }
            }
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
                ShowNotification("Error", $"❌ Hiba a kérdések betöltésekor: {ex.Message}", 5000);
            }
            finally
            {
                StartNoteQuizCommand.RaiseCanExecuteChanged();
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
                // ✅ ELLENŐRZÉS: Van-e kérdés ÉS megváltozott-e a tartalom?
                bool contentChanged = Note.Id > 0 && Content != _originalContent;
                bool hasExistingQuestions = HasQuestions && Questions.Count > 0;

                if (contentChanged && hasExistingQuestions)
                {
                    var questionCount = Questions.Count;
                    var result = System.Windows.MessageBox.Show(
                        $"⚠️ FIGYELEM!\n\n" +
                        $"A jegyzet tartalma megváltozott.\n" +
                        $"Az ehhez kapcsolódó {questionCount} db generált kérdés törlődni fog!\n\n" +
                        $"Biztosan folytatod a mentést?",
                        "Kérdések törlése",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);

                    if (result != System.Windows.MessageBoxResult.Yes)
                    {
                        ShowNotification("Info", "ℹ️ Mentés megszakítva", 3000);
                        return;
                    }

                    // ✅ KÉRDÉSEK TÖRLÉSE AZ ADATBÁZISBÓL!
                    await _questionListVM.DeleteAllQuestionsForCurrentNoteAsync();
                }

                // ✅ Mentés végrehajtása
                NoteSaved?.Invoke(Note);
                HasUnsavedChanges = false;

                // ✅ Sikeres mentés értesítés
                if (contentChanged && hasExistingQuestions)
                {
                    ShowNotification("Success", $"✅ Jegyzet mentve! Kérdések törölve.", 4000);
                }
                else
                {
                    ShowNotification("Success", "✅ Jegyzet sikeresen mentve!", 3000);
                }

                // ✅ Eredeti tartalom frissítése mentés után
                _originalContent = Content;

                if (DetectMarkdownContent(Content))
                {
                    IsMarkdownContent = true;
                }

                if (Note.Id > 0)
                {
                    await LoadQuestionsAsync();
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Error", $"❌ Hiba a mentés során: {ex.Message}", 5000);
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
            _originalContent = Content;
        }

        public async Task RefreshQuestionsAsync()
        {
            if (Note?.Id > 0)
            {
                await LoadQuestionsAsync();
            }
        }

        private async void ImportPdfAsync()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Válassz PDF dokumentumot",
                Filter = "PDF fájlok (*.pdf)|*.pdf|Minden fájl (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                var filePath = openFileDialog.FileName;
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                IsImporting = true;
                ImportStatus = "📄 PDF beolvasása folyamatban...";

                var extractedText = await _documentImportService.ExtractTextFromPdfAsync(filePath);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    ShowNotification("Warning", "⚠️ A PDF fájl üres vagy nem tartalmaz szöveget", 4000);
                    System.Windows.MessageBox.Show(
                        "A PDF fájl üres vagy nem tartalmaz szöveget.",
                        "Figyelmeztetés",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    ImportStatus = "⚠️ A PDF üres";
                    return;
                }

                if (Note.Id == 0 || string.IsNullOrWhiteSpace(Content))
                {
                    if (string.IsNullOrWhiteSpace(Title) || Title == "Új jegyzet")
                    {
                        Title = fileName;
                    }
                    Content = extractedText;
                    ImportStatus = $"✅ {fileName} sikeresen importálva!";
                    ShowNotification("Success", $"✅ PDF sikeresen importálva! ({extractedText.Length:N0} karakter)", 4000);
                }
                else
                {
                    var result = System.Windows.MessageBox.Show(
                        $"A jegyzet már tartalmaz szöveget.\n\n" +
                        $"• IGEN: Hozzáfűzés a meglévő tartalomhoz\n" +
                        $"• NEM: Teljes csere az új tartalomra\n" +
                        $"• MÉGSE: Nem importál",
                        "Import mód",
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        Content += "\n\n" + extractedText;
                        ImportStatus = $"✅ {fileName} hozzáfűzve!";
                        ShowNotification("Success", $"✅ PDF tartalom hozzáfűzve!", 4000);
                    }
                    else if (result == System.Windows.MessageBoxResult.No)
                    {
                        Content = extractedText;
                        ImportStatus = $"✅ {fileName} lecserélve!";
                        ShowNotification("Success", $"✅ Tartalom lecserélve PDF-re!", 4000);
                    }
                    else
                    {
                        ImportStatus = "❌ Import megszakítva";
                        ShowNotification("Info", "ℹ️ Import megszakítva", 3000);
                        return;
                    }
                }

                IsMarkdownContent = true;
                HasUnsavedChanges = true;
                SaveCommand.RaiseCanExecuteChanged();

                System.Windows.MessageBox.Show(
                    $"PDF sikeresen importálva!\n\n" +
                    $"Fájl: {fileName}\n" +
                    $"Karakterek: {extractedText.Length:N0}\n\n" +
                    $"💡 Tipp: Használd a 'Szerkesztés' gombot a tartalom módosításához.",
                    "Sikeres Import",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (FileNotFoundException ex)
            {
                ShowNotification("Error", "❌ Fájl nem található", 5000);
                System.Windows.MessageBox.Show(
                    $"A fájl nem található:\n{ex.Message}",
                    "Hiba",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                ImportStatus = "❌ Fájl nem található";
            }
            catch (InvalidOperationException ex)
            {
                ShowNotification("Error", "❌ PDF feldolgozási hiba", 5000);
                System.Windows.MessageBox.Show(
                    $"Nem sikerült a PDF feldolgozása:\n{ex.Message}",
                    "Hiba",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                ImportStatus = "❌ PDF feldolgozási hiba";
            }
            catch (Exception ex)
            {
                ShowNotification("Error", $"❌ Váratlan hiba: {ex.Message}", 5000);
                System.Windows.MessageBox.Show(
                    $"Váratlan hiba történt:\n{ex.Message}",
                    "Hiba",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                ImportStatus = "❌ Import sikertelen";
            }
            finally
            {
                IsImporting = false;
                await Task.Delay(3000);
                if (!IsImporting)
                {
                    ImportStatus = string.Empty;
                }
            }
        }

        private bool DetectMarkdownContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            return (content.Contains("$$") && content.Contains("\\")) ||
                   (content.Contains("###") && content.Length > 200) ||
                   (content.Contains("##") && content.Length > 200) ||
                   (content.Contains("**") && content.Length > 500);
        }
    }
}