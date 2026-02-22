using Core.Entities;
using Core.Interfaces.Services;
using Data.Services;
using System.Windows;
using WPF.ViewModels.Notes;
using WPF.ViewModels.Subjects;
using WPF.ViewModels.Topics;

public class CrudOperationHandler
{
    private readonly ISubjectService _subjectService;
    private readonly ITopicService _topicService;
    private readonly INoteService _noteService;
    private readonly IQuestionService _questionService;

    private readonly SubjectListViewModel _subjectsVM;
    private readonly TopicListViewModel _topicsVM;
    private readonly NoteListViewModel _notesVM;

    public CrudOperationHandler(
        ISubjectService subjectService,
        ITopicService topicService,
        INoteService noteService,
        IQuestionService questionService,
        SubjectListViewModel subjectsVM,
        TopicListViewModel topicsVM,
        NoteListViewModel notesVM)
    {
        _subjectService = subjectService;
        _topicService = topicService;
        _noteService = noteService;
        _questionService = questionService;
        _subjectsVM = subjectsVM;
        _topicsVM = topicsVM;
        _notesVM = notesVM;
    }

    public async Task SaveSubjectAsync(Subject subject)
    {
        try
        {
            if (subject == null)
                throw new ArgumentNullException(nameof(subject));

            Subject savedSubject;

            if (subject.Id > 0)
            {
                // Update
                savedSubject = await _subjectService.UpdateSubjectAsync(subject);
            }
            else
            {
                // Add
                savedSubject = await _subjectService.AddSubjectAsync(subject.Title);
            }

            // UI frissítés
            await _subjectsVM.LoadSubjectsAsync();

        }
        catch (InvalidOperationException ex)
        {
            // Duplikáció
            MessageBox.Show(ex.Message, "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
            throw;
        }
        catch (ArgumentException ex)
        {
            // Validáció
            MessageBox.Show(ex.Message, "Érvénytelen adat", MessageBoxButton.OK, MessageBoxImage.Warning);
            throw;
        }
        catch (Exception ex)
        {
            // Egyéb hiba
            MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    public async Task SaveTopicAsync(Topic topic)
    {
        try
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            Topic savedTopic;

            if (topic.Id > 0)
            {
                savedTopic = await _topicService.UpdateTopicAsync(topic);
            }
            else
            {
                savedTopic = await _topicService.AddTopicAsync(topic);
            }

            // UI frissítés
            if (_subjectsVM.SelectedSubject != null)
            {
                await _topicsVM.LoadTopicsAsync(_subjectsVM.SelectedSubject.Id);
            }
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
            throw;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Érvénytelen adat", MessageBoxButton.OK, MessageBoxImage.Warning);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(ex.Message, "Hozzáférés megtagadva", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    public async Task DeleteTopicAsync(int topicId)
    {
        try
        {
            // Megerősítés
            var result = MessageBox.Show(
                "Biztosan törölni szeretnéd ezt a témakört?\n\nA hozzá tartozó Jegyzetek és Kérdések is törlődnek!",
                "Törlés megerősítése",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await _topicService.DeleteTopicAsync(topicId);

            // UI frissítés
            _topicsVM.RemoveTopic(topicId);

            if (_topicsVM.SelectedTopic?.Id == topicId)
            {
                _topicsVM.SelectedTopic = null;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(ex.Message, "Hozzáférés megtagadva", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Érvénytelen művelet", MessageBoxButton.OK, MessageBoxImage.Warning);
            throw;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a törlés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    public async Task DeleteSubjectAsync(int subjectId)
    {
        try
        {
            var result = MessageBox.Show(
                "Biztosan törölni szeretnéd ezt a tantárgyat?\n\nA hozzá tartozó Témakörök is törlődnek!",
                "Törlés megerősítése",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await _subjectService.DeleteSubjectAsync(subjectId);

            _subjectsVM.RemoveSubject(subjectId);

            if (_subjectsVM.SelectedSubject?.Id == subjectId)
            {
                _subjectsVM.SelectedSubject = null;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a törlés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    public async Task DeleteNoteAsync(int noteId)
    {
        try
        {
            await _noteService.DeleteNoteAsync(noteId);

            _notesVM.RemoveNoteFromList(noteId);

            if (_notesVM.SelectedNote?.Note.Id == noteId)
            {
                _notesVM.SelectedNote = null;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a jegyzet törlése során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    public async Task SaveQuestionAsync(Question question)
    {
        try
        {
            if (question == null) throw new ArgumentNullException(nameof(question));
            await _questionService.SaveQuestionsAsync(new List<Question> { question });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        try
        {
            var result = MessageBox.Show("Biztosan törölni szeretnéd ezt a kérdést?", "Kérdés törlése",
                                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _questionService.DeleteQuestionAsync(questionId);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a törlés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}