using Core.Entities;
using Core.Repositories;

namespace MemoSphere.Core.Interfaces
{
    public interface INoteRepository : IGenericRepository<Note>
    {
        Task<IEnumerable<Note>> GetNotesByTopicIdAsync(int topicId);
        Task<IEnumerable<Note>> GetAllNotesWithTopicsAndSubjectsAsync();
    }
}