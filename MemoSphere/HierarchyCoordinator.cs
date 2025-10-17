using WPF.ViewModels.Notes;
using WPF.ViewModels.Questions;
using WPF.ViewModels.Quiz;
using WPF.ViewModels.Subjects;
using WPF.ViewModels.Topics;

public class HierarchyCoordinator
{
    private readonly SubjectListViewModel _subjectsVM;
    private readonly TopicListViewModel _topicsVM;
    private readonly NoteListViewModel _notesVM;
    private readonly QuestionListViewModel _questionsVM;
    private readonly NoteDetailViewModel _noteDetailVM;
    private readonly QuizViewModel _quizVM;

    public HierarchyCoordinator(
        SubjectListViewModel subjectsVM,
        TopicListViewModel topicsVM,
        NoteListViewModel notesVM,
        QuestionListViewModel questionsVM,
        NoteDetailViewModel noteDetailVM,
        QuizViewModel quizVM)
    {
        _subjectsVM = subjectsVM;
        _topicsVM = topicsVM;
        _notesVM = notesVM;
        _questionsVM = questionsVM;
        _noteDetailVM = noteDetailVM;
        _quizVM = quizVM;

    }

    private void SetupHierarchyEvents()
    {
        // Subject → Topic
        _subjectsVM.SubjectSelected += async selectedSubjectVM =>
        {
            if (selectedSubjectVM != null)
            {
                await _topicsVM.LoadTopicsAsync(selectedSubjectVM.Id);
            }
            else
            {
                _topicsVM.ClearTopics();
                _notesVM.ClearNotes();
            }

            _noteDetailVM.SetCurrentNote(null);
        };

        // Topic → Note
        _topicsVM.TopicSelected += async selectedTopicVM =>
        {
            _noteDetailVM.SetCurrentNote(null);

            var selectedIds = new List<int>();

            if (selectedTopicVM != null)
            {
                _noteDetailVM.SelectedTopicId = selectedTopicVM.Id;
                await _notesVM.LoadNotesAsync(selectedTopicVM.Id);
            }
            else
            {
                _noteDetailVM.SelectedTopicId = 0;
                _notesVM.ClearNotes();
            }
            await _quizVM.ValidateTopicsForQuizAsync(selectedIds);
        };

        // Note → Questions
        _notesVM.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(_notesVM.SelectedNote))
            {
                _noteDetailVM.SetCurrentNote(_notesVM.SelectedNote?.Note);

                if (_notesVM.SelectedNote?.Note != null)
                {
                    await _questionsVM.LoadQuestionsAsync(_notesVM.SelectedNote.Note.Id);
                }
                else
                {
                    _questionsVM.ClearQuestions();
                }
            }
        };

    }
    public void Initialize()
    {
        SetupHierarchyEvents();
    }

}