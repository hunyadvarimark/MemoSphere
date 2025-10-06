using Core.Context;
using Core.Entities;
using MemoSphere.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{

    public class NoteRepository : GenericRepository<Note>, INoteRepository
    {
        public NoteRepository(MemoSphereDbContext context) : base(context) { }

        public async Task<IEnumerable<Note>> GetNotesByTopicIdAsync(int topicId)
        {
            return await _context.Set<Note>().Where(n => n.TopicId == topicId).ToListAsync();
        }

        public async Task<IEnumerable<Note>> GetAllNotesWithTopicsAndSubjectsAsync()
        {
            return await _context.Set<Note>()
                .Include(n => n.Topic)
                .ThenInclude(t => t.Subject)
                .ToListAsync();
        }
    }
}