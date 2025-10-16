using Core.Entities;
using Core.Interfaces.Services;


namespace Data.Services
{
    public class SubjectService : ISubjectService
    {

        private readonly IUnitOfWork _unitOfWork;

        public SubjectService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }

        // Add a new subject
        public async Task<Subject> AddSubjectAsync(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A tantárgy címe nem lehet üres.", nameof(title));
            }

            if (title.Length > 100)
            {
                throw new ArgumentException("A tantárgy címe maximum 100 karakter hosszú lehet.", nameof(title));
            }

            var subject = new Subject
            {
                Title = title
            };

            await _unitOfWork.Subjects.AddAsync(subject);
            await _unitOfWork.SaveChangesAsync();

            return subject;
        }

        // Delete a subject by id
        public async Task DeleteSubjectAsync(int id)
        {
            var subjectToDelete = await _unitOfWork.Subjects.GetByIdAsync(id);

            if (subjectToDelete == null)
            {
                throw new ArgumentException("A megadott azonosítóval nem található tantárgy.", nameof(id));
            }

            _unitOfWork.Subjects.Remove(subjectToDelete);
            await _unitOfWork.SaveChangesAsync();
        }

        // Get a subject by id
        public Task<Subject> GetSubjectByIdAsync(int id)
        {
            return _unitOfWork.Subjects.GetByIdAsync(id);
        }

        // Get all subjects
        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            return await _unitOfWork.Subjects.GetAllAsync();
        }
        
        // Get all subjects with topics
        public async Task<IEnumerable<Subject>> GetAllSubjectsWithTopicsAsync()
        {
            return await _unitOfWork.Subjects.GetFilteredAsync(
                filter: s => s.Topics.Any(),
                includeProperties: "Topics",
                orderBy: q => q.OrderBy(s => s.Title));
        }

        // Check if a subject exists by title
        public async Task<bool> SubjectExistsAsync(string title, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A tantárgy címe nem lehet üres.", nameof(title));
            }

            string lowerTitle = title.ToLower();

            var subjects = await _unitOfWork.Subjects.GetFilteredAsync(
                filter: s => s.Title.ToLower() == lowerTitle &&
                              (!excludeId.HasValue || s.Id != excludeId.Value)
            );

            return subjects.Any();
        }

        // Update a subject
        public async Task<Subject> UpdateSubjectAsync(Subject subject)
        {
            _unitOfWork.Subjects.Update(subject);
            await _unitOfWork.SaveChangesAsync();
            return subject;
        }
    }
}
