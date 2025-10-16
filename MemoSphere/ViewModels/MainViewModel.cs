using Core.Interfaces.Services;
using Core.Services;
using System.Windows.Input;
using WPF.Utilities;
using WPF.ViewModels;
using WPF.ViewModels.Notes;
using WPF.ViewModels.Questions;
using WPF.ViewModels.Quiz;
using WPF.ViewModels.Subjects;
using WPF.ViewModels.Topics;
using WPF.Views;
using WPF.Views.Quiz;

public class MainViewModel : BaseViewModel
{
    private readonly HierarchyCoordinator _hierarchyCoordinator;
    private readonly CrudOperationHandler _crudHandler;

    public QuizViewModel QuizVM { get; }

    // List ViewModels
    public SubjectListViewModel SubjectsVM { get; }
    public TopicListViewModel TopicsVM { get; }
    public NoteListViewModel NotesVM { get; }
    public QuestionListViewModel QuestionsVM { get; set; }

    // Detail ViewModels
    public NoteDetailViewModel AddNoteVM { get; }
    public SubjectDetailViewModel SubjectDetailVM { get; }
    public TopicDetailViewModel TopicDetailVM { get; }
    public QuestionDetailViewModel QuestionDetailVM { get; }

    // UI State
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


    // Commands - inicializálás a konstruktorban
    public ICommand UnselectNoteCommand { get; }
    public RelayCommand AddSubjectCommand { get; }
    public RelayCommand AddTopicCommand { get; }
    public AsyncCommand<object> GenerateQuestionsCommand { get; }
    public ICommand StartQuizCommand { get; }
    public RelayCommand CloseQuizCommand { get; }

    public MainViewModel(
    SubjectListViewModel subjectsVM,
    TopicListViewModel topicsVM,
    NoteListViewModel notesVM,
    QuestionListViewModel questionsVM,
    QuizViewModel quizVM,
    SubjectDetailViewModel subjectDetailVM,
    TopicDetailViewModel topicDetailVM,
    NoteDetailViewModel noteDetailVM,
    HierarchyCoordinator hierarchyCoordinator,
    CrudOperationHandler crudHandler)
    {
        // ViewModels hozzárendelése
        SubjectsVM = subjectsVM ?? throw new ArgumentNullException(nameof(subjectsVM));
        TopicsVM = topicsVM ?? throw new ArgumentNullException(nameof(topicsVM));
        NotesVM = notesVM ?? throw new ArgumentNullException(nameof(notesVM));
        QuestionsVM = questionsVM ?? throw new ArgumentNullException(nameof(questionsVM));
        QuizVM = quizVM ?? throw new ArgumentNullException(nameof(quizVM));

        AddNoteVM = noteDetailVM ?? throw new ArgumentNullException(nameof(noteDetailVM));
        SubjectDetailVM = subjectDetailVM ?? throw new ArgumentNullException(nameof(subjectDetailVM));
        TopicDetailVM = topicDetailVM ?? throw new ArgumentNullException(nameof(topicDetailVM));

        // Commands inicializálása
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

        GenerateQuestionsCommand = new AsyncCommand<object>(
            async param =>
            {
                if (param is int noteId)
                    await QuestionsVM.GenerateQuestionsForNoteAsync(noteId);
            },
            param => param is int id && id > 0
        );

        StartQuizCommand = new RelayCommand(
            async _ => await StartQuizAsync(),
            _ => TopicsVM.SelectedTopic != null && !IsQuizActive
        );
        
        CloseQuizCommand = new RelayCommand(
            _ => IsQuizActive = false,
            _ => IsQuizActive
        );

        _hierarchyCoordinator = hierarchyCoordinator ?? throw new ArgumentNullException(nameof(hierarchyCoordinator));
        _crudHandler = crudHandler ?? throw new ArgumentNullException(nameof(crudHandler));

        _hierarchyCoordinator.Initialize();

        SetupEventSubscriptions();
    }
    private async Task StartQuizAsync()
    {
        try
        {
            if (TopicsVM?.SelectedTopic == null)
            {
                System.Windows.MessageBox.Show("Válassz egy témakört!", "Figyelmeztetés",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (QuizVM == null)
            {
                System.Windows.MessageBox.Show("QuizVM nem inicializálódott!", "Hiba",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var topicIds = new List<int> { TopicsVM.SelectedTopic.Id };

            // Kvíz betöltése
            await QuizVM.LoadQuizCommand.ExecuteAsync(topicIds);

            // Ellenőrizd hogy sikerült-e betölteni a kérdéseket
            if (QuizVM.QuizItems == null || !QuizVM.QuizItems.Any())
            {
                System.Windows.MessageBox.Show("Nem sikerült betölteni a kvíz kérdéseket.", "Hiba",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Kvíz ablak megnyitása a UI thread-en
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var quizWindow = new QuizWindow(QuizVM);
                quizWindow.ShowDialog();
            });

            IsQuizActive = false;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Hiba a kvíz indításakor:\n\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                "Hiba",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
    private void SetupEventSubscriptions()
    {
        // Save events
        SubjectDetailVM.SubjectSavedRequested += async subject =>
        {
            await _crudHandler.SaveSubjectAsync(subject);
            IsAddingSubject = false;
            SubjectDetailVM.ResetState();
        };

        TopicDetailVM.TopicSavedRequested += async topic =>
        {
            await _crudHandler.SaveTopicAsync(topic);
            IsAddingTopic = false;
            TopicDetailVM.ResetState(SubjectsVM.SelectedSubject?.Id ?? 0);
        };

        AddNoteVM.NoteSavedRequested += async note =>
        {
            await _crudHandler.SaveNoteAsync(note);
            NotesVM.SelectedNote = null;
        };

        // Delete events
        SubjectsVM.DeleteSubjectRequested += async id => await _crudHandler.DeleteSubjectAsync(id);
        TopicsVM.DeleteTopicRequested += async id => await _crudHandler.DeleteTopicAsync(id);
        NotesVM.DeleteNoteRequested += async id => await _crudHandler.DeleteNoteAsync(id);
        AddNoteVM.NoteDeleteRequested += async id =>
        {
            await _crudHandler.DeleteNoteAsync(id);
            AddNoteVM.ResetState();
            if (TopicsVM.SelectedTopic != null)
            {
                AddNoteVM.SelectedTopicId = TopicsVM.SelectedTopic.Id;
            }
        };

        // Cancel events
        SubjectDetailVM.CancelRequested += () => IsAddingSubject = false;
        TopicDetailVM.CancelRequested += () => IsAddingTopic = false;

        // Edit events
        SubjectsVM.EditSubjectRequested += subject =>
        {
            SubjectDetailVM.LoadSubject(subject);
            IsAddingSubject = true;
        };

        TopicsVM.EditTopicRequested += topic =>
        {
            TopicDetailVM.LoadTopic(topic);
            IsAddingTopic = true;
        };

        // UI State synchronization
        SubjectsVM.SubjectSelected += _ =>
        {
            IsAddingTopic = false;
            IsAddingSubject = false;
        };
        
        QuizVM.CloseRequested += () =>
        {
            IsQuizActive = false;
        };

        QuizVM.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(QuizVM.IsQuizFinished) && QuizVM.IsQuizFinished)
            {
                IsQuizActive = false;
            }
        };
        
        TopicsVM.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TopicsVM.SelectedTopic) && IsQuizActive)
            {
                IsQuizActive = false;
            }
        };

    }

    public async Task InitializeAsync()
    {
        await SubjectsVM.LoadSubjectsAsync();
    }
}