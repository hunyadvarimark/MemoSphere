using Core.Entities;
using Core.Interfaces.Services;
using Data.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPF.Utilities;
using WPF.ViewModels.Dashboard;
using WPF.ViewModels.Notes;
using WPF.ViewModels.Questions;
using WPF.ViewModels.Quiz;
using WPF.ViewModels.Subjects;
using WPF.ViewModels.Topics;
using WPF.Views.Quiz;

namespace WPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly HierarchyCoordinator _hierarchyCoordinator;
        private readonly CrudOperationHandler _crudHandler;
        private readonly INoteService _noteService;
        private readonly IQuestionService _questionService;
        private readonly IAuthService _authService;
        private readonly IDocumentImportService _documentImportService;
        public QuizViewModel QuizVM { get; }
        public QuizTopicSelectionViewModel QuizSelectionVM { get; }
        public DashboardViewModel DashboardVM { get; }
        // List ViewModels
        public SubjectListViewModel SubjectsVM { get; }
        public TopicListViewModel TopicsVM { get; }
        public NoteListViewModel NotesVM { get; }
        // Detail ViewModels
        public SubjectDetailViewModel SubjectDetailVM { get; }
        public TopicDetailViewModel TopicDetailVM { get; }
        // TAB KEZELÉS
        public ObservableCollection<NoteTabViewModel> OpenNotes { get; } = new();
        private NoteTabViewModel _activeNote;
        public NoteTabViewModel ActiveNote
        {
            get => _activeNote;
            set
            {
                if (_activeNote != null)
                    _activeNote.IsActive = false;
                if (SetProperty(ref _activeNote, value))
                {
                    if (_activeNote != null)
                        _activeNote.IsActive = true;
                }
            }
        }
        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    TopicsVM.FilterTopics(value);
                }
            }
        }
        private bool _isNoteListVisible;
        public bool IsNoteListVisible
        {
            get => _isNoteListVisible;
            set => SetProperty(ref _isNoteListVisible, value);
        }
        private string _currentUserEmail;
        public string CurrentUserEmail
        {
            get => _currentUserEmail;
            set => SetProperty(ref _currentUserEmail, value);
        }
        // UI State
        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        private bool _isAddingSubject;
        public bool IsAddingSubject
        {
            get => _isAddingSubject;
            set => SetProperty(ref _isAddingSubject, value);
        }
        private bool _isAddingTopic;
        public bool IsAddingTopic
        {
            get => _isAddingTopic;
            set => SetProperty(ref _isAddingTopic, value);
        }
        private bool _isQuizActive;
        public bool IsQuizActive
        {
            get => _isQuizActive;
            set => SetProperty(ref _isQuizActive, value);
        }
        private bool _hasEnoughQuestions;
        public bool HasEnoughQuestions
        {
            get => _hasEnoughQuestions;
            set
            {
                if (SetProperty(ref _hasEnoughQuestions, value))
                {
                    System.Diagnostics.Debug.WriteLine($"🔔 HasEnoughQuestions changed to: {value}");
                    ((RelayCommand)StartQuizCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isQuizSelectionVisible;
        public bool IsQuizSelectionVisible
        {
            get => _isQuizSelectionVisible;
            set => SetProperty(ref _isQuizSelectionVisible, value);
        }
        // Commands
        public ICommand UnselectNoteCommand { get; }
        public RelayCommand AddSubjectCommand { get; }
        public RelayCommand AddTopicCommand { get; }
        public ICommand StartQuizCommand { get; }
        public RelayCommand CloseQuizCommand { get; }
        public RelayCommand AddNewNoteCommand { get; }
        public RelayCommand ToggleNoteListCommand { get; }
        public RelayCommand OpenNoteCommand { get; }
        public ICommand LogoutCommand { get; }
        public MainViewModel(
            SubjectListViewModel subjectsVM,
            TopicListViewModel topicsVM,
            NoteListViewModel notesVM,
            QuizViewModel quizVM,
            SubjectDetailViewModel subjectDetailVM,
            TopicDetailViewModel topicDetailVM,
            DashboardViewModel dashboardVM,
            QuizTopicSelectionViewModel quizSelectionVM,
            HierarchyCoordinator hierarchyCoordinator,
            CrudOperationHandler crudHandler,
            INoteService noteService,
            IQuestionService questionService,
            IAuthService authService,
            IDocumentImportService documentImportService)
        {
            // ViewModels
            SubjectsVM = subjectsVM ?? throw new ArgumentNullException(nameof(subjectsVM));
            TopicsVM = topicsVM ?? throw new ArgumentNullException(nameof(topicsVM));
            NotesVM = notesVM ?? throw new ArgumentNullException(nameof(notesVM));
            QuizVM = quizVM ?? throw new ArgumentNullException(nameof(quizVM));
            DashboardVM = dashboardVM ?? throw new ArgumentNullException(nameof(dashboardVM));
            SubjectDetailVM = subjectDetailVM ?? throw new ArgumentNullException(nameof(subjectDetailVM));
            TopicDetailVM = topicDetailVM ?? throw new ArgumentNullException(nameof(topicDetailVM));
            QuizSelectionVM = quizSelectionVM ?? throw new ArgumentNullException(nameof(quizSelectionVM));
            QuizSelectionVM.Initialize(this);
            
            
            _hierarchyCoordinator = hierarchyCoordinator ?? throw new ArgumentNullException(nameof(hierarchyCoordinator));
            _crudHandler = crudHandler ?? throw new ArgumentNullException(nameof(crudHandler));
            _noteService = noteService ?? throw new ArgumentNullException(nameof(noteService));
            _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _documentImportService = documentImportService ?? throw new ArgumentNullException(nameof(documentImportService));
            _hasEnoughQuestions = false;
            // Commands
            UnselectNoteCommand = new RelayCommand(_ => NotesVM.SelectedNote = null);
            AddSubjectCommand = new RelayCommand(_ =>
            {
                SubjectDetailVM.ResetState();
                IsAddingSubject = true;
            });
            AddTopicCommand = new RelayCommand(_ =>
            {
                TopicDetailVM.ResetState(SubjectsVM.SelectedSubject?.Id ?? 0);
                IsAddingTopic = true;
            }, _ => SubjectsVM.SelectedSubject != null);
            AddNewNoteCommand = new RelayCommand(_ => CreateNewNote(), _ => TopicsVM.SelectedTopic != null);
            ToggleNoteListCommand = new RelayCommand(_ => IsNoteListVisible = !IsNoteListVisible);
            StartQuizCommand = new RelayCommand(
                async _ => await OpenQuizSelectionModalAsync(),
                _ => {
                    var canExecute = SubjectsVM.SelectedSubject != null && !IsQuizActive;
                    return canExecute;
                }
            );
            CloseQuizCommand = new RelayCommand(
                _ => IsQuizActive = false,
                _ => IsQuizActive
            );
            OpenNoteCommand = new RelayCommand(param =>
            {
                if (param is Note note)
                {
                    OpenNoteInTab(note);
                    IsNoteListVisible = false;
                }
            });
            LogoutCommand = new RelayCommand(async _ => await LogoutAsync());
            CurrentUserEmail = _authService.GetCurrentUserEmail() ?? "Ismeretlen";
            _hierarchyCoordinator.Initialize();
            SetupEventSubscriptions();
        }
        private void CreateNewNote()
        {
            if (TopicsVM.SelectedTopic == null) return;
            var newNote = new Note
            {
                TopicId = TopicsVM.SelectedTopic.Id,
                Title = "Új jegyzet",
                Content = ""
            };
            OpenNoteInTab(newNote);
        }
        private void OpenNoteInTab(Note note)
        {
            var existingTab = OpenNotes.FirstOrDefault(t => t.Note.Id == note.Id && note.Id > 0);
            if (existingTab != null)
            {
                ActiveNote = existingTab;
                return;
            }
            var questionListVM = new QuestionListViewModel(_questionService);
            var noteTab = new NoteTabViewModel(note, _noteService, questionListVM, _documentImportService, this);
            noteTab.CloseRequested += OnNoteTabCloseRequested;
            noteTab.NoteSaved += OnNoteTabSaved;
            noteTab.ActivateRequested += tab => ActiveNote = tab;
            noteTab.NoteQuizRequested += async (noteId) => await StartNoteQuizAsync(noteId);

            OpenNotes.Add(noteTab);
            ActiveNote = noteTab;
            if (note.Id > 0)
            {
                _ = questionListVM.LoadQuestionsAsync(note.Id);
            }
        }
        private void OnNoteTabCloseRequested(NoteTabViewModel tab)
        {
            OpenNotes.Remove(tab);
            if (ActiveNote == tab)
            {
                ActiveNote = OpenNotes.LastOrDefault();
            }
        }
        private async void OnNoteTabSaved(Note note)
        {
            try
            {
                Note savedNote;
                if (note.Id == 0)
                {
                    savedNote = await _noteService.AddNoteAsync(note);
                    var tab = OpenNotes.FirstOrDefault(t => t.Note == note);
                    if (tab != null)
                    {
                        tab.Note = savedNote;
                        tab.MarkAsSaved();
                        await tab.RefreshQuestionsAsync();
                    }
                }
                else
                {
                    savedNote = await _noteService.UpdateNoteAsync(note);
                    var tab = OpenNotes.FirstOrDefault(t => t.Note.Id == note.Id);
                    tab?.MarkAsSaved();
                }
                if (TopicsVM.SelectedTopic != null)
                {
                    await NotesVM.LoadNotesAsync(TopicsVM.SelectedTopic.Id);
                    await ValidateQuestionCountAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba");
            }
        }
        private async Task OpenQuizSelectionModalAsync()
        {
            if (SubjectsVM.SelectedSubject == null)
            {
                MessageBox.Show("Válassz ki egy tantárgyat a listából.", "Nincs kiválasztva tantárgy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Töltsük be a modalba az aktuális tárgy témaköreit
                await QuizSelectionVM.LoadTopicsAsync(SubjectsVM.SelectedSubject.Id);

                // Nyissuk meg a modalt
                IsQuizSelectionVisible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a kvíz-választó betöltésekor: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async Task StartNoteQuizAsync(int noteId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🚀 StartNoteQuizAsync called for NoteID: {noteId}");

                QuizVM.ResetState();
                System.Diagnostics.Debug.WriteLine("🔄 QuizVM state reset before loading");

                await QuizVM.LoadQuizFromNoteCommand.ExecuteAsync(noteId);

                if (QuizVM.QuizItems == null || !QuizVM.QuizItems.Any())
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Nem sikerült kérdéseket betölteni a jegyzet-kvízhez.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Note Quiz loaded with {QuizVM.QuizItems.Count} questions");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var quizWindow = new QuizWindow(QuizVM);
                    quizWindow.ShowDialog();
                });

                IsQuizActive = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in StartNoteQuizAsync: {ex.Message}");
                MessageBox.Show(
                    $"Hiba a jegyzet-kvíz indításakor:\n\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void SetupEventSubscriptions()
        {
            // Hozzáadva: Subscription a topic activation változásra, hogy frissítse a dashboard-ot
            TopicsVM.TopicActivationChanged += async () =>
            {
                System.Diagnostics.Debug.WriteLine("🔄 Topic activation changed - Refreshing dashboard");
                await DashboardVM.LoadDashboardDataAsync();
            };

            // Save events
            SubjectDetailVM.SubjectSavedRequested += async subject =>
            {
                try
                {
                    await _crudHandler.SaveSubjectAsync(subject);
                    IsAddingSubject = false;
                    IsDialogOpen = false;
                    SubjectDetailVM.ResetState();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Subject save error: {ex.Message}");
                }
            };

            TopicDetailVM.TopicSavedRequested += async topic =>
            {
                try
                {
                    await _crudHandler.SaveTopicAsync(topic);
                    IsAddingTopic = false;
                    IsDialogOpen = false;
                    TopicDetailVM.ResetState(SubjectsVM.SelectedSubject?.Id ?? 0);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Topic save error: {ex.Message}");
                }
            };
            // Delete events
            SubjectsVM.DeleteSubjectRequested += async id =>
            {
                try
                {
                    await _crudHandler.DeleteSubjectAsync(id);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Subject delete error: {ex.Message}");
                }
            };
            TopicsVM.DeleteTopicRequested += async id =>
            {
                try
                {
                    await _crudHandler.DeleteTopicAsync(id);
                    var tabsToRemove = OpenNotes.Where(t => t.Note.TopicId == id).ToList();
                    foreach (var tab in tabsToRemove)
                    {
                        OnNoteTabCloseRequested(tab);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Topic delete error: {ex.Message}");
                }
            };
            NotesVM.DeleteNoteRequested += async id =>
            {
                try
                {
                    await _crudHandler.DeleteNoteAsync(id);
                    var openTab = OpenNotes.FirstOrDefault(t => t.Note.Id == id);
                    if (openTab != null)
                    {
                        OnNoteTabCloseRequested(openTab);
                    }
                    await ValidateQuestionCountAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Note delete error: {ex.Message}");
                }
            };
            // Cancel events
            SubjectDetailVM.CancelRequested += () =>
            {
                IsAddingSubject = false;
                IsDialogOpen = false;
            };
            TopicDetailVM.CancelRequested += () =>
            {
                IsAddingTopic = false;
                IsDialogOpen = false;
            };
            // Edit events
            SubjectsVM.EditSubjectRequested += subject =>
            {
                IsDialogOpen = true;
                SubjectDetailVM.LoadSubject(subject);
                IsAddingSubject = true;
            };
            TopicsVM.EditTopicRequested += topic =>
            {
                IsDialogOpen = true;
                TopicDetailVM.LoadTopic(topic);
                IsAddingTopic = true;
            };
            // UI State synchronization
            SubjectsVM.SubjectSelected += _ =>
            {
                IsAddingTopic = false;
                IsAddingSubject = false;
                OpenNotes.Clear();
                ActiveNote = null;
            };
            TopicsVM.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(TopicsVM.SelectedTopic))
                {
                    System.Diagnostics.Debug.WriteLine($"📖 Topic changed to: {TopicsVM.SelectedTopic?.Title}");
                    if (TopicsVM.SelectedTopic != null)
                    {
                        await ValidateQuestionCountAsync();
                    }
                    else
                    {
                        HasEnoughQuestions = false;
                    }
                    if (IsQuizActive)
                    {
                        IsQuizActive = false;
                    }
                }
            };
            // Note lista note kiválasztás -> Tab megnyitása
            NotesVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NotesVM.SelectedNote) && NotesVM.SelectedNote != null)
                {
                    OpenNoteInTab(NotesVM.SelectedNote.Note);
                    IsNoteListVisible = false;
                }
            };
            QuizVM.CloseRequested += () =>
            {
                IsQuizActive = false;
            };
            QuizVM.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(QuizVM.IsQuizFinished) && QuizVM.IsQuizFinished)
                {
                    IsQuizActive = false;
                    await DashboardVM.LoadDashboardDataAsync();
                }
            };
        }
        private async Task ValidateQuestionCountAsync()
        {
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("🔍 ValidateQuestionCountAsync STARTED");
            if (TopicsVM?.SelectedTopic == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No topic selected");
                HasEnoughQuestions = false;
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                return;
            }
            try
            {
                var topicIds = new List<int> { TopicsVM.SelectedTopic.Id };
                System.Diagnostics.Debug.WriteLine($"📚 Topic: {TopicsVM.SelectedTopic.Title} (ID: {TopicsVM.SelectedTopic.Id})");
                System.Diagnostics.Debug.WriteLine($"🔍 Calling QuizVM.ValidateTopicsForQuizAsync...");
                await QuizVM.ValidateTopicsForQuizAsync(topicIds);
                System.Diagnostics.Debug.WriteLine($"✅ ValidateTopicsForQuizAsync completed");
                System.Diagnostics.Debug.WriteLine($"🎯 QuizVM.CanStartQuiz = {QuizVM.CanStartQuiz}");
                var oldValue = HasEnoughQuestions;
                HasEnoughQuestions = QuizVM.CanStartQuiz;
                System.Diagnostics.Debug.WriteLine($"📊 HasEnoughQuestions: {oldValue} → {HasEnoughQuestions}");
                // ✅ FORCE UI UPDATE
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(HasEnoughQuestions));
                    ((RelayCommand)StartQuizCommand).RaiseCanExecuteChanged();
                    System.Diagnostics.Debug.WriteLine("🔄 UI forced to update");
                });
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                HasEnoughQuestions = false;
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
        }
        public async Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine("🏁 InitializeAsync started");
            CurrentUserEmail = _authService.GetCurrentUserEmail() ?? "Ismeretlen";
            System.Diagnostics.Debug.WriteLine($"👤 Current user email: {CurrentUserEmail}");
            await SubjectsVM.LoadSubjectsAsync();
            // Hozzáadva: Dashboard inicializálása az app indításakor
            await DashboardVM.LoadDashboardDataAsync();
            if (SubjectsVM.Subjects.Any())
            {
                SubjectsVM.SelectedSubject = SubjectsVM.Subjects.First();
                await Task.Delay(100);
                if (TopicsVM.SelectedTopic != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 Initial validation for topic: {TopicsVM.SelectedTopic.Title}");
                    await ValidateQuestionCountAsync();
                }
            }
            System.Diagnostics.Debug.WriteLine("✅ InitializeAsync complete");
        }
        private async Task LogoutAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Logout kezdődik ===");
                // Megerősítés
                var result = MessageBox.Show(
                    "Biztosan ki szeretnél jelentkezni?",
                    "Kijelentkezés",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
                // Kijelentkezés
                await _authService.SignOutAsync();
                System.Diagnostics.Debug.WriteLine("Session törölve");
                // Restart application
                System.Diagnostics.Process.Start(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
                );
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout hiba: {ex.Message}");
                MessageBox.Show($"Hiba a kijelentkezés során: {ex.Message}", "Hiba");
            }
        }
    }
}