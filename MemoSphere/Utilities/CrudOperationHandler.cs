using Core.Entities;
using Core.Interfaces.Services;

public class CrudOperationHandler
{
    private readonly ISubjectService _subjectService;
    private readonly ITopicService _topicService;
    private readonly INoteService _noteService;
    private readonly IQuestionService _questionService;
    private readonly IDialogService _dialogService;
    public CrudOperationHandler(
        ISubjectService subjectService,
        ITopicService topicService,
        INoteService noteService,
        IQuestionService questionService,
        IDialogService dialogService)
    {
        _subjectService = subjectService;
        _topicService = topicService;
        _noteService = noteService;
        _questionService = questionService;
        _dialogService = dialogService;
    }

    public async Task<Subject> SaveSubjectAsync(Subject subject)
    {
        try
        {
            if (subject == null)
                throw new ArgumentNullException(nameof(subject));

            if (subject.Id > 0)
            {
                return await _subjectService.UpdateSubjectAsync(subject);
            }
            else
            {
                return await _subjectService.AddSubjectAsync(subject.Title);
            }
        }
        catch (InvalidOperationException ex)
        {
            _dialogService.ShowMessage(ex.Message, "Figyelmeztetés", MessageType.Warning);
            throw;
        }
        catch (ArgumentException ex)
        {
            _dialogService.ShowMessage(ex.Message, "Érvénytelen adat", MessageType.Warning);
            throw;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Hiba a mentés során: {ex.Message}", "Hiba", MessageType.Error);
            throw;
        }
    }

    public async Task<Topic> SaveTopicAsync(Topic topic)
    {
        try
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            if (topic.Id > 0)
            {
                return await _topicService.UpdateTopicAsync(topic);
            }
            else
            {
                return await _topicService.AddTopicAsync(topic);
            }
        }
        catch (InvalidOperationException ex)
        {
            _dialogService.ShowMessage(ex.Message, "Figyelmeztetés", MessageType.Warning);
            throw;
        }
        catch (ArgumentException ex)
        {
            _dialogService.ShowMessage(ex.Message, "Érvénytelen adat", MessageType.Warning);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _dialogService.ShowMessage(ex.Message, "Hozzáférés megtagadva", MessageType.Error);
            throw;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Hiba a mentés során: {ex.Message}", "Hiba", MessageType.Error);
            throw;
        }
    }

    public async Task<bool> DeleteTopicAsync(int topicId)
    {
        try
        {
            var result = _dialogService.AskConfirmation(
                "Biztosan törölni szeretnéd ezt a témakör?\\n\\nA hozzá tartozó Jegyzetek és Kérdések is törlődnek!",
                "Törlés megerősítése");

            if (result != DialogResult.Yes)
                return false;

            await _topicService.DeleteTopicAsync(topicId);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _dialogService.ShowMessage(ex.Message, "Hozzáférés megtagadva", MessageType.Error);
            throw;
        }
        catch (ArgumentException ex)
        {
            _dialogService.ShowMessage(ex.Message, "Érvénytelen művelet", MessageType.Warning);
            throw;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Hiba a törlés során: {ex.Message}", "Hiba", MessageType.Error);
            throw;
        }
    }

    public async Task<bool> DeleteSubjectAsync(int subjectId)
    {
        try
        {
            var result = _dialogService.AskConfirmation(
                "Biztosan törölni szeretnéd ezt a tantárgyat?\\n\\nA hozzá tartozó Témakörök is törlődnek!",
                "Törlés megerősítése");

            if (result != DialogResult.Yes)
                return false;

            await _subjectService.DeleteSubjectAsync(subjectId);
            return true;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Hiba a törlés során: {ex.Message}", "Hiba", MessageType.Error);
            throw;
        }
    }

    public async Task<bool> DeleteNoteAsync(int noteId)
    {
        try
        {
            await _noteService.DeleteNoteAsync(noteId);
            return true;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Hiba a jegyzet törlése során: {ex.Message}", "Hiba", MessageType.Error);
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
            _dialogService.ShowMessage($"Hiba a mentés során: {ex.Message}", "Hiba", MessageType.Error);
            throw;
        }
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        try
        {
            var result = _dialogService.AskConfirmation("Biztosan törölni szeretnéd ezt a kérdést?", "Kérdés törlése");

            if (result == DialogResult.Yes)
            {
                await _questionService.DeleteQuestionAsync(questionId);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Hiba a törlés során: {ex.Message}", "Hiba", MessageType.Error);
            return false;
        }
    }
}