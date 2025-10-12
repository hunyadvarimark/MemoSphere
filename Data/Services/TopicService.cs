using Core.Entities;
using Core.Interfaces.Services;
using System.Linq.Expressions; // Kelleni fog a GetFilteredAsync-hez

namespace Data.Services
{
    public class TopicService : ITopicService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TopicService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // --- CREATE ---
        public async Task<Topic> AddTopicAsync(Topic topic) // 🏆 Módosítva Task<Topic>-ra
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            // 1. Validáció
            if (string.IsNullOrWhiteSpace(topic.Title))
            {
                throw new ArgumentException("A téma neve nem lehet üres.", nameof(topic));
            }
            if (topic.Title.Length > 100)
            {
                throw new ArgumentException("A téma neve maximum 100 karakter hosszú lehet.", nameof(topic));
            }
            if (topic.SubjectId <= 0)
            {
                throw new ArgumentException("A tantárgy azonosítója érvénytelen.", nameof(topic.SubjectId));
            }

            // 2. Szülő Subject ellenőrzése (referenciális integritás)
            var subjectExists = await _unitOfWork.Subjects.GetByIdAsync(topic.SubjectId) != null;
            if (!subjectExists)
            {
                throw new ArgumentException("A megadott tantárgyazonosító érvénytelen.", nameof(topic.SubjectId));
            }

            // 3. Hozzáadás és Mentés
            await _unitOfWork.Topics.AddAsync(topic);
            await _unitOfWork.SaveChangesAsync();

            return topic; // 🏆 Visszaadjuk a mentett Topic-ot
        }

        // --- DELETE ---
        public async Task DeleteTopicAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(id));
            }
            var topicToDelete = await _unitOfWork.Topics.GetByIdAsync(id);

            if (topicToDelete == null)
            {
                throw new ArgumentException("A megadott témakör nem található.", nameof(id));
            }

            // Meglévő, erős kaszkádolt törlési logika (helyes)
            var notesToDelete = await _unitOfWork.Notes.GetFilteredAsync(n => n.TopicId == id);
            var questionsToDelete = await _unitOfWork.Questions.GetFilteredAsync(q => q.TopicId == id);

            foreach (var question in questionsToDelete)
            {
                var answersToDelete = await _unitOfWork.Answers.GetFilteredAsync(a => a.QuestionId == question.Id);
                _unitOfWork.Answers.RemoveRange(answersToDelete);
            }

            _unitOfWork.Notes.RemoveRange(notesToDelete);
            _unitOfWork.Questions.RemoveRange(questionsToDelete);

            _unitOfWork.Topics.Remove(topicToDelete);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateTopicAsync(Topic topic)
        {
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

            _unitOfWork.Topics.Update(topic);
            await _unitOfWork.SaveChangesAsync();
        }


        // --- READ/QUERY Metódusok ---

        public async Task<Topic> GetTopicByIdAsync(int id)
        {
            return await _unitOfWork.Topics.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Topic>> GetTopicBySubjectIdAsync(int subjectId)
        {
            if (subjectId <= 0)
            {
                throw new ArgumentException("A tantárgy azonosítója érvénytelen.", nameof(subjectId));
            }

            return await _unitOfWork.Topics.GetFilteredAsync(
                filter: t => t.SubjectId == subjectId,
                orderBy: q => q.OrderBy(t => t.Title)
            );
        }

        public async Task<IEnumerable<Topic>> GetTopicsWithNotesAndQuestionsAsync()
        {
            return await _unitOfWork.Topics.GetFilteredAsync(
                includeProperties: "Notes,Questions"
            );
        }

        public async Task<Topic> GetTopicWithNotesAndQuestionsAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(id));
            }

            var topics = await _unitOfWork.Topics.GetFilteredAsync(
                filter: t => t.Id == id,
                includeProperties: "Notes,Questions"
            );

            return topics.FirstOrDefault();
        }

        // 🏆 KIEGÉSZÍTÉS: TopicExistsAsync metódus a duplikáció ellenőrzéséhez (ViewModel hívja)
        public async Task<bool> TopicExistsAsync(string title, int subjectId, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(title)) return false;

            string lowerTitle = title.Trim().ToLower();

            return await _unitOfWork.Topics.ExistsAsync(
                        t => t.Title.ToLower() == lowerTitle &&
                             t.SubjectId == subjectId &&
                             (!excludeId.HasValue || t.Id != excludeId.Value)
            );
        }
    }
}