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

        public ObservableCollection<Question> Questions => _questionListVM.Questions;

        public IEnumerable<Question> DistinctQuestions => Questions.GroupBy(q => q.Text).Select(g => g.First());

        // Commands
        public AsyncCommand<object> SaveCommand { get; }
        public RelayCommand CloseCommand { get; }
        public AsyncCommand<object> GenerateQuestionsCommand { get; }
        public RelayCommand ActivateCommand { get; }
        public RelayCommand ImportPdfCommand { get; }
        public RelayCommand ToggleEditModeCommand { get; }

        // Events
        public event Action<NoteTabViewModel> CloseRequested;
        public event Action<Note> NoteSaved;
        public event Action<NoteTabViewModel> ActivateRequested;

        public NoteTabViewModel(
            Note note,
            INoteService noteService,
            QuestionListViewModel questionListVM,
            IDocumentImportService documentImportService)
        {
            _note = note ?? throw new ArgumentNullException(nameof(note));
            _noteService = noteService ?? throw new ArgumentNullException(nameof(noteService));
            _questionListVM = questionListVM ?? throw new ArgumentNullException(nameof(questionListVM));
            _documentImportService = documentImportService ?? throw new ArgumentNullException(nameof(documentImportService));

            SaveCommand = new AsyncCommand<object>(SaveNoteAsync, CanSave);
            CloseCommand = new RelayCommand(_ => RequestClose());
            GenerateQuestionsCommand = _questionListVM.GenerateQuestionsCommand;
            ActivateCommand = new RelayCommand(_ => ActivateRequested?.Invoke(this));
            ImportPdfCommand = new RelayCommand(_ => ImportPdfAsync());
            ToggleEditModeCommand = new RelayCommand(_ => ToggleEditMode());

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
            };

            bool hasMarkdown = DetectMarkdownContent(note?.Content);
            IsMarkdownContent = hasMarkdown;

            // Ha van markdown, preview mód (edit mode = false), különben edit mód (edit mode = true)
            _isInEditMode = !hasMarkdown;
        }

        private void ToggleEditMode()
        {
            IsMarkdownContent = !IsMarkdownContent;

            // Ha szerkesztési módba váltunk és van markdown, adjunk figyelmeztetést
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
                    IsMarkdownContent = true; // Visszaváltunk preview-ra
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

                // ✅ Mentés után preview módba, HA van markdown
                if (DetectMarkdownContent(Content))
                {
                    IsMarkdownContent = true; // Ez automatikusan beállítja _isInEditMode = false
                }

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
                    }
                    else if (result == System.Windows.MessageBoxResult.No)
                    {
                        Content = extractedText;
                        ImportStatus = $"✅ {fileName} lecserélve!";
                    }
                    else
                    {
                        ImportStatus = "❌ Import megszakítva";
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
                System.Windows.MessageBox.Show(
                    $"A fájl nem található:\n{ex.Message}",
                    "Hiba",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                ImportStatus = "❌ Fájl nem található";
            }
            catch (InvalidOperationException ex)
            {
                System.Windows.MessageBox.Show(
                    $"Nem sikerült a PDF feldolgozása:\n{ex.Message}",
                    "Hiba",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                ImportStatus = "❌ PDF feldolgozási hiba";
            }
            catch (Exception ex)
            {
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

            return (content.Contains("$$") && content.Contains("\\")) || // LaTeX
                   (content.Contains("###") && content.Length > 200) ||    // Markdown címek
                   (content.Contains("##") && content.Length > 200) ||
                   (content.Contains("**") && content.Length > 500);      // Markdown bold (hosszú szövegben)
        }
    }
}