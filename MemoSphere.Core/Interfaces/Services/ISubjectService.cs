using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface ISubjectService
    {
        Task<IEnumerable<Subject>> GetAllSubjectsAsync();
        Task<IEnumerable<Subject>> GetAllSubjectsWithTopicsAsync();
        Task<Subject> GetSubjectByIdAsync(int id);
        Task<Subject> AddSubjectAsync(string title);
        Task DeleteSubjectAsync(int id);
        Task<bool> SubjectExistsAsync(string title, int? excludeId = null);
        Task UpdateSubjectAsync(Subject subject);
    }
}
