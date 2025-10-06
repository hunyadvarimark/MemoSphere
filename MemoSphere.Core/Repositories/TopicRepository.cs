using Core.Context;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface ITopicRepository : IGenericRepository<Topic>
    {
        Task<IEnumerable<Topic>> GetTopicsWithNotesAndQuestionsAsync();
        Task<Topic> GetTopicWithNotesAndQuestionsAsync(int id);
        Task<IEnumerable<Topic>> GetTopicsBySubjectIdAsync(int subjectId);
    }

    public class TopicRepository : GenericRepository<Topic>, ITopicRepository
    {
        public TopicRepository(MemoSphereDbContext context) : base(context) { }

        public async Task<IEnumerable<Topic>> GetTopicsBySubjectIdAsync(int subjectId)
        {
            return await _context.Set<Topic>()
                .Where(t => t.SubjectId == subjectId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Topic>> GetTopicsWithNotesAndQuestionsAsync()
        {
            return await _context.Set<Topic>()
                .Include(t => t.Notes)
                .Include(t => t.Questions)
                .ToListAsync();
        }

        public async Task<Topic> GetTopicWithNotesAndQuestionsAsync(int id)
        {
            return await _context.Set<Topic>()
                .Include(t => t.Notes)
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}