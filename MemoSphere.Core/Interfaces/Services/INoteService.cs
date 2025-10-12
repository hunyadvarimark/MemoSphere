using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface INoteService
    {
        Task<int> AddNoteAsync(Note note);
        Task<IEnumerable<Note>> GetNotesByTopicIdAsync(int topicId);
        Task DeleteNoteAsync(int id);
        Task UpdateNoteAsync(Note note);

    }
}
