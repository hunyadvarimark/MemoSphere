using Core.Entities;
using Core.Interfaces.Services;
using System.Windows;
using WPF.ViewModels.Notes;
using WPF.ViewModels.Subjects;
using WPF.ViewModels.Topics;

public class CrudOperationHandler
{
    private readonly ISubjectService _subjectService;
    private readonly ITopicService _topicService;
    private readonly INoteService _noteService;

    private readonly SubjectListViewModel _subjectsVM;
    private readonly TopicListViewModel _topicsVM;
    private readonly NoteListViewModel _notesVM;

    public CrudOperationHandler(
        ISubjectService subjectService,
        ITopicService topicService,
        INoteService noteService,
        SubjectListViewModel subjectsVM,
        TopicListViewModel topicsVM,
        NoteListViewModel notesVM)
    {
        _subjectService = subjectService;
        _topicService = topicService;
        _noteService = noteService;
        _subjectsVM = subjectsVM;
        _topicsVM = topicsVM;
        _notesVM = notesVM;
    }

    public async Task SaveSubjectAsync(Subject subject)
    {
        try
        {
            if (subject == null)
            {
                MessageBox.Show("Hiba: mentésre jelölt tantárgy üres.");
                return;
            }

            Subject savedSubject = subject.Id > 0
                ? await _subjectService.UpdateSubjectAsync(subject)
                : await _subjectService.AddSubjectAsync(subject.Title);

            await _subjectsVM.LoadSubjectsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a tantárgy mentése során: {ex.Message}");
        }
    }

    public async Task SaveTopicAsync(Topic topic)
    {
        try
        {
            Topic savedTopic = topic.Id > 0
                ? await _topicService.UpdateTopicAsync(topic)
                : await _topicService.AddTopicAsync(topic);

            if (_subjectsVM.SelectedSubject != null)
            {
                await _topicsVM.LoadTopicsAsync(_subjectsVM.SelectedSubject.Id);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a témakör mentése során: {ex.Message}");
        }
    }

    public async Task SaveNoteAsync(Note note)
    {
        try
        {
            Note savedNote = note.Id > 0
                ? await _noteService.UpdateNoteAsync(note)
                : await _noteService.AddNoteAsync(note);

            if (_topicsVM.SelectedTopic != null)
            {
                await _notesVM.LoadNotesAsync(_topicsVM.SelectedTopic.Id);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a jegyzet mentése során: {ex.Message}");
        }
    }

    public async Task DeleteSubjectAsync(int subjectId)
    {
        try
        {
            await _subjectService.DeleteSubjectAsync(subjectId);
            _subjectsVM.RemoveSubject(subjectId);

            if (_subjectsVM.SelectedSubject?.Id == subjectId)
                _subjectsVM.SelectedSubject = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a tantárgy törlésekor: {ex.Message}");
        }
    }

    public async Task DeleteTopicAsync(int topicId)
    {
        try
        {
            await _topicService.DeleteTopicAsync(topicId);
            _topicsVM.RemoveTopic(topicId);
            _topicsVM.SelectedTopic = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a témakör törlésekor: {ex.Message}");
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
            MessageBox.Show($"Hiba a jegyzet törlése során: {ex.Message}");
        }
    }
}