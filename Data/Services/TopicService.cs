using Core.Entities;
using Core.Interfaces.Services;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Data.Context;

namespace Data.Services
{
    public class TopicService : ITopicService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly IDbContextFactory<MemoSphereDbContext> _factory;

        public TopicService(IUnitOfWork unitOfWork, IAuthService authService, IDbContextFactory<MemoSphereDbContext> factory)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _factory = factory;
        }

        public async Task<Topic> AddTopicAsync(Topic topic)
        {
            var userId = _authService.GetCurrentUserId();

            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (string.IsNullOrWhiteSpace(topic.Title))
            {
                throw new ArgumentException("A téma neve nem lehet üres.", nameof(topic));

            }
            if (await TopicExistsAsync(topic.Title, topic.SubjectId))
            {
                throw new InvalidOperationException($"Már létezik '{topic.Title}' nevű tantárgy!");
            }
            if (topic.Title.Length > 100)
            {
                throw new ArgumentException("A téma neve maximum 100 karakter hosszú lehet.", nameof(topic));
            }
            if (topic.SubjectId <= 0)
            {
                throw new ArgumentException("A tantárgy azonosítója érvénytelen.", nameof(topic.SubjectId));
            }

            var subject = await _unitOfWork.Subjects.GetByIdAsync(topic.SubjectId);
            if (subject == null || subject.UserId != userId)
            {
                throw new ArgumentException("A megadott tantárgyazonosító érvénytelen vagy nincs jogosultság.", nameof(topic.SubjectId));
            }

            topic.UserId = userId;

            await _unitOfWork.Topics.AddAsync(topic);

            return topic;
        }
        public async Task DeleteTopicAsync(int id)
        {
            var userId = _authService.GetCurrentUserId();

            if (id <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(id));
            }

            var topics = await _unitOfWork.Topics.GetFilteredAsync(
                filter: t => t.Id == id,
                includeProperties: "Subject" // Betöltjük a Subject entitást
            );
            var topicToDelete = topics.FirstOrDefault();

            if (topicToDelete == null || topicToDelete.Subject == null || topicToDelete.Subject.UserId != userId)
            {
                throw new ArgumentException("A megadott témakör nem található vagy nincs jogosultság.", nameof(id));
            }

            // Tranzakció indítása a kapcsolódó adatok (jegyzetek, kérdések, válaszok) atomi törléséhez.
            using var context = _factory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // 1. Töröljük a felhasználó jegyzeteit a témához
                var notesToDelete = await context.Notes
                    .Where(n => n.TopicId == id && n.UserId == userId)
                    .ToListAsync();

                // 2. Töröljük a témához tartozó kérdéseket.
                // Itt nem kell ellenőrizni a UserId-t, mivel a topicToDelete ellenőrzés már megtörtént, 
                // de a tiszta biztonság kedvéért érdemes lehet az ellenőrzést hagyni, ahogy az eredeti kódban volt:
                var questionsToDelete = await context.Questions
                    .Where(q => q.TopicId == id && q.Topic.Subject.UserId == userId)
                    .ToListAsync();

                // 3. Töröljük a válaszokat a törlendő kérdésekhez.
                foreach (var question in questionsToDelete)
                {
                    var answersToDelete = await context.Answers
                        .Where(a => a.QuestionId == question.Id)
                        .ToListAsync();
                    context.Answers.RemoveRange(answersToDelete);
                }

                // Töröljük a kérdéseket és a jegyzeteket
                context.Notes.RemoveRange(notesToDelete);
                context.Questions.RemoveRange(questionsToDelete);

                // 4. Töröljük magát a témát
                context.Topics.Remove(topicToDelete);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Topic> UpdateTopicAsync(Topic topic)
        {
            var userId = _authService.GetCurrentUserId();

            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            if (topic.Id <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(topic.Id));
            }
            if (string.IsNullOrWhiteSpace(topic.Title))
            {
                throw new ArgumentException("A téma neve nem lehet üres.", nameof(topic));
            }
            if (topic.Title.Length > 100)
            {
                throw new ArgumentException("A téma neve maximum 100 karakter hosszú lehet.", nameof(topic));
            }

            var existingTopic = await GetTopicByIdAsync(topic.Id);
            if (existingTopic == null)
            {
                throw new ArgumentException("A téma nem található vagy nincs jogosultság.", nameof(topic.Id));
            }

            _unitOfWork.Topics.Update(topic);

            await _unitOfWork.Topics.ReloadAsync(topic);

            return topic;
        }

        public async Task<Topic> GetTopicByIdAsync(int id)
        {
            var userId = _authService.GetCurrentUserId();

            var topics = await _unitOfWork.Topics.GetFilteredAsync(t => t.Id == id && t.Subject.UserId == userId);
            return topics.FirstOrDefault();
        }

        public async Task<IEnumerable<Topic>> GetTopicBySubjectIdAsync(int subjectId)
        {
            var userId = _authService.GetCurrentUserId();

            if (subjectId <= 0)
            {
                throw new ArgumentException("A tantárgy azonosítója érvénytelen.", nameof(subjectId));
            }

            return await _unitOfWork.Topics.GetFilteredAsync(
                filter: t => t.SubjectId == subjectId && t.Subject.UserId == userId,
                orderBy: q => q.OrderBy(t => t.Title)
            );
        }

        public async Task<IEnumerable<Topic>> GetTopicsWithNotesAndQuestionsAsync()
        {
            var userId = _authService.GetCurrentUserId();

            return await _unitOfWork.Topics.GetFilteredAsync(
                filter: t => t.Subject.UserId == userId,
                includeProperties: "Notes,Questions"
            );
        }

        public async Task<Topic> GetTopicWithNotesAndQuestionsAsync(int id)
        {
            var userId = _authService.GetCurrentUserId();

            if (id <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(id));
            }

            var topics = await _unitOfWork.Topics.GetFilteredAsync(
                filter: t => t.Id == id && t.Subject.UserId == userId,
                includeProperties: "Notes,Questions"
            );

            return topics.FirstOrDefault();
        }

        public async Task<bool> TopicExistsAsync(string title, int subjectId, int? excludeId = null)
        {
            var userId = _authService.GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(title)) return false;

            string lowerTitle = title.Trim().ToLower();

            return await _unitOfWork.Topics.ExistsAsync(
                        t => t.Title.ToLower() == lowerTitle &&
                             t.SubjectId == subjectId &&
                             (!excludeId.HasValue || t.Id != excludeId.Value) &&
                             t.Subject.UserId == userId
            );
        }
    }
}