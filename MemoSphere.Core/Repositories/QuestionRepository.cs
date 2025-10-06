using Core.Context;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IQuestionRepository : IGenericRepository<Question>
    {
        Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId);
        Task<IEnumerable<Question>> GetQuestionsWithAnswersAsync();
    }

    public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
    {
        public QuestionRepository(MemoSphereDbContext context) : base(context) { }

        public async Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId)
        {
            return await _context.Set<Question>().Where(q => q.TopicId == topicId).ToListAsync();
        }

        public async Task<IEnumerable<Question>> GetQuestionsWithAnswersAsync()
        {
            return await _context.Set<Question>().Include(q => q.Answers).ToListAsync();
        }
    }
}