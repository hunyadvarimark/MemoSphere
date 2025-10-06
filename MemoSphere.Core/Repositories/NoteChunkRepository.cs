using Core.Context;
using Core.Entities;
using Microsoft.EntityFrameworkCore;


namespace Core.Repositories
{   
    public interface INoteChunkRepository : IGenericRepository<NoteChunk>
    {
        Task<IEnumerable<NoteChunk>> GetNoteChunksByNoteIdAsync(int noteId);
    }
    public class NoteChunkRepository : GenericRepository<NoteChunk>, INoteChunkRepository
    {
        public NoteChunkRepository(MemoSphereDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<NoteChunk>> GetNoteChunksByNoteIdAsync(int noteId)
        {
            return await _context.Set<NoteChunk>()
                                .Where(nc => nc.NoteId == noteId)
                                .ToListAsync();
        }
    }
}
