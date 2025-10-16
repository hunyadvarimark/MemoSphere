using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface INoteService
    {
        Task<Note> AddNoteAsync(Note note);
        Task<IEnumerable<Note>> GetNotesByTopicIdAsync(int topicId);
        Task DeleteNoteAsync(int id);
        Task<Note> UpdateNoteAsync(Note note);

    }
}
