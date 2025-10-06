using Core.Entities;

namespace MemoSphere.Core.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<Subject>> GetAllSubjectsAsync();
        Task<IEnumerable<Subject>> GetAllSubjectsWithTopicsAsync();
        Task<Subject> GetSubjectByIdAsync(int id);
        Task AddSubjectAsync(string title);
        Task DeleteSubjectAsync(int id);

    }
}
