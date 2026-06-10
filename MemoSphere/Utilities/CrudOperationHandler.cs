using Core.Entities;
using Core.Interfaces.Services;
using System.Windows;

public class CrudOperationHandler
{
    private readonly ISubjectService _subjectService;
    private readonly ITopicService _topicService;
    private readonly INoteService _noteService;
    private readonly IQuestionService _questionService;


    public CrudOperationHandler(
        ISubjectService subjectService,
        ITopicService topicService,
        INoteService noteService,
        IQuestionService questionService)
    {
        _subjectService = subjectService;
        _topicService = topicService;
        _noteService = noteService;
        _questionService = questionService;
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
            MessageBox.Show(ex.Message, "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
            throw;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Érvénytelen adat", MessageBoxButton.OK, MessageBoxImage.Warning);
            throw;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
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

    public async Task<bool> DeleteTopicAsync(int topicId)
    {
        try
        {
            var result = MessageBox.Show(
                "Biztosan törölni szeretnéd ezt a témakört?\n\nA hozzá tartozó Jegyzetek és Kérdések is törlődnek!",
                "Törlés megerősítése",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return false;

            await _topicService.DeleteTopicAsync(topicId);
            return true;
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

    public async Task<bool> DeleteSubjectAsync(int subjectId)
    {
        try
        {
            var result = MessageBox.Show(
                "Biztosan törölni szeretnéd ezt a tantárgyat?\n\nA hozzá tartozó Témakörök is törlődnek!",
                "Törlés megerősítése",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return false;

            await _subjectService.DeleteSubjectAsync(subjectId);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a törlés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
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