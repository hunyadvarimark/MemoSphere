using Core.Context;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface ISubjectRepository : IGenericRepository<Subject>
    {
        Task<IEnumerable<Subject>> GetAllSubjectsWithTopicsAsync();
    }

    public class SubjectRepository : GenericRepository<Subject>, ISubjectRepository
    {
        public SubjectRepository(MemoSphereDbContext context) : base(context) { }

        public async Task<IEnumerable<Subject>> GetAllSubjectsWithTopicsAsync()
        {
            return await _context.Set<Subject>().Include(s => s.Topics).ToListAsync();
        }
    }
}