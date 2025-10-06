using Core.Context;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IAnswerRepository : IGenericRepository<Answer>
    {
        Task<IEnumerable<Answer>> GetAnswersByQuestionIdAsync(int questionId);
    }

    public class AnswerRepository : GenericRepository<Answer>, IAnswerRepository
    {
        public AnswerRepository(MemoSphereDbContext context) : base(context) { }

        public async Task<IEnumerable<Answer>> GetAnswersByQuestionIdAsync(int questionId)
        {
            return await _context.Set<Answer>().Where(a => a.QuestionId == questionId).ToListAsync();
        }
    }
}