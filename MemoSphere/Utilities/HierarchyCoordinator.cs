using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        _subjectsVM = subjectsVM ?? throw new ArgumentNullException(nameof(subjectsVM));
        _topicsVM = topicsVM ?? throw new ArgumentNullException(nameof(topicsVM));
        _notesVM = notesVM ?? throw new ArgumentNullException(nameof(notesVM));
        _questionsVM = questionsVM ?? throw new ArgumentNullException(nameof(questionsVM));
        _noteDetailVM = noteDetailVM ?? throw new ArgumentNullException(nameof(noteDetailVM));
        _quizVM = quizVM ?? throw new ArgumentNullException(nameof(quizVM));
    }

    private void SetupHierarchyEvents()
    {
        // Subject → Topic
        _subjectsVM.SubjectSelected += async selectedSubjectVM =>
        {
            // Távolítsuk el a Task.Yield()-et – felesleges és ronthat az async flow-n
            if (selectedSubjectVM != null)
            {
                await _topicsVM.LoadTopicsAsync(selectedSubjectVM.Id);

                // 💡 JAVÍTÁS 1: Kiválasztjuk az első témakört, ha van.
                if (_topicsVM.Topics.Any())
                {
                    _topicsVM.SelectedTopic = _topicsVM.Topics.First();
                }
                else
                {
                    _topicsVM.SelectedTopic = null;
                }
            }
            else
            {
                _topicsVM.ClearTopics();
                _notesVM.ClearNotes();
                _topicsVM.SelectedTopic = null;
            }

            _noteDetailVM.SetCurrentNote(null);
        };

        // Topic → Note
        _topicsVM.TopicSelected += async selectedTopicVM =>
        {
            _noteDetailVM.SetCurrentNote(null);
            // Távolítsuk el a Task.Yield()-et

            var selectedIds = new List<int>();

            if (selectedTopicVM != null)
            {
                _noteDetailVM.SelectedTopicId = selectedTopicVM.Id;
                await _notesVM.LoadNotesAsync(selectedTopicVM.Id);

                // 💡 JAVÍTÁS 2: Kiválasztjuk az első jegyzetet, ha van.
                if (_notesVM.Notes.Any())
                {
                    _notesVM.SelectedNote = _notesVM.Notes.First();
                }
                else
                {
                    _notesVM.SelectedNote = null;
                }

                selectedIds.Add(selectedTopicVM.Id);
            }
            else
            {
                _noteDetailVM.SelectedTopicId = 0;
                _notesVM.ClearNotes();
                _notesVM.SelectedNote = null;
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