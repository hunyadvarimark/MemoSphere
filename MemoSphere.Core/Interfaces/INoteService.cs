using Core.Entities;

namespace MemoSphere.Core.Interfaces
{
    public interface INoteService
    {
        Task AddNoteAsync(Note note);
        Task<IEnumerable<Note>> GetNotesByTopicIdAsync(int topicId);
        Task DeleteNoteAsync(int id);
        Task UpdateNoteAsync(Note note);

    }
}
