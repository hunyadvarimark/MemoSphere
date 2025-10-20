using Core.Entities;
using Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Data.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;  // Inject auth service

        public SubjectService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        // Add a new subject
        public async Task<Subject> AddSubjectAsync(string title)
        {
            Guid userId;
            try
            {
                userId = _authService.GetCurrentUserId();  // Get current user ID
                Console.WriteLine($"Adding subject '{title}' for user: {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba az AddSubjectAsync userId lekéréskor: {ex.Message}");
                throw;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A tantárgy címe nem lehet üres.", nameof(title));
            }
            if (await SubjectExistsAsync(title))
                throw new InvalidOperationException($"Már létezik '{title}' nevű tantárgy!");
            
            if (title.Length > 100)
            {
                throw new ArgumentException("A tantárgy címe maximum 100 karakter hosszú lehet.", nameof(title));
            }

            var subject = new Subject
            {
                Title = title,
                UserId = userId  // Set UserId
            };

            await _unitOfWork.Subjects.AddAsync(subject);
            // Távolítsuk el, mivel a repository ment

            return subject;
        }

        // Delete a subject by id
        public async Task DeleteSubjectAsync(int id)
        {
            Guid userId;
            try
            {
                userId = _authService.GetCurrentUserId();  // Get current user ID
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a DeleteSubjectAsync userId lekéréskor: {ex.Message}");
                throw;
            }

            var subjectToDelete = await _unitOfWork.Subjects.GetByIdAsync(id);

            if (subjectToDelete == null || subjectToDelete.UserId != userId)  // Check if belongs to user
            {
                throw new ArgumentException("A megadott azonosítóval nem található tantárgy vagy nincs jogosultság.", nameof(id));
            }

            _unitOfWork.Subjects.Remove(subjectToDelete);
            // Távolítsuk el, mivel a Remove ment
        }

        // Get a subject by id
        public async Task<Subject> GetSubjectByIdAsync(int id)
        {
            Guid userId;
            try
            {
                userId = _authService.GetCurrentUserId();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a GetSubjectByIdAsync userId lekéréskor: {ex.Message}");
                throw;
            }

            var subjects = await _unitOfWork.Subjects.GetFilteredAsync(s => s.Id == id && s.UserId == userId);
            return subjects.FirstOrDefault();
        }

        // Get all subjects
        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            Guid userId;
            try
            {
                userId = _authService.GetCurrentUserId();
                Console.WriteLine($"GetAllSubjectsAsync for user: {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllSubjectsAsync hiba userId lekéréskor: {ex.Message}");
                return Enumerable.Empty<Subject>();
            }

            // Remove the if (userId == Guid.Empty) since if parse succeeds, it's not empty
            return await _unitOfWork.Subjects.GetFilteredAsync(s => s.UserId == userId);
        }

        // Get all subjects with topics
        public async Task<IEnumerable<Subject>> GetAllSubjectsWithTopicsAsync()
        {
            Guid userId;
            try
            {
                userId = _authService.GetCurrentUserId();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllSubjectsWithTopicsAsync hiba: {ex.Message}");
                return Enumerable.Empty<Subject>();
            }

            return await _unitOfWork.Subjects.GetFilteredAsync(
                filter: s => s.Topics.Any() && s.UserId == userId,  // Filter by user
                includeProperties: "Topics",
                orderBy: q => q.OrderBy(s => s.Title));
        }

        // Check if a subject exists by title
        public async Task<bool> SubjectExistsAsync(string title, int? excludeId = null)
        {
            Guid userId;
            try
            {
                userId = _authService.GetCurrentUserId();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubjectExistsAsync hiba: {ex.Message}");
                throw;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A tantárgy címe nem lehet üres.", nameof(title));
            }

            string lowerTitle = title.ToLower();

            var subjects = await _unitOfWork.Subjects.GetFilteredAsync(
                filter: s => s.Title.ToLower() == lowerTitle &&
                              (!excludeId.HasValue || s.Id != excludeId.Value) &&
                              s.UserId == userId  // Filter by user
            );

            return subjects.Any();
        }

        // Update a subject
        public async Task<Subject> UpdateSubjectAsync(Subject subject)
        {
            Guid userId;
            try
            {
                userId = _authService.GetCurrentUserId();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateSubjectAsync hiba: {ex.Message}");
                throw;
            }

            var existing = await GetSubjectByIdAsync(subject.Id);  // Használja a filteres Get-et
            if (existing == null)
            {
                throw new ArgumentException("A tantárgy nem található vagy nincs jogosultság.", nameof(subject.Id));
            }

            existing.Title = subject.Title;  // Update fields

            _unitOfWork.Subjects.Update(existing);
            // Távolítsuk el, mivel az Update ment
            return existing;
        }
    }
}