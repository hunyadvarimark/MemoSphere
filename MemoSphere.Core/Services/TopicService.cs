using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using MemoSphere.Core.Interfaces;

namespace MemoSphere.Core.Services
{

    public class TopicService : ITopicService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TopicService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task AddTopicAsync(string name, int subjectId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A téma neve nem lehet üres.", nameof(name));
            }
            if(name.Length > 100)
            {
                throw new ArgumentException("A téma neve maximum 100 karakter hosszú lehet.", nameof(name));
            }
            if(subjectId <= 0)
            {
                throw new ArgumentException("A tantárgy azonosítója érvénytelen.", nameof(subjectId));
            }
            var subjectExists = await _unitOfWork.Subjects.GetByIdAsync(subjectId) != null;
            if (!subjectExists)
            {
                throw new ArgumentException("A megadott tantárgyazonosító érvénytelen.", nameof(subjectId));
            }
            var topic = new Topic
            {
                Name = name,
                SubjectId = subjectId
            };
            await _unitOfWork.Topics.AddAsync(topic);
            await _unitOfWork.SaveChangesAsync();
            
        }

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
            var notesToDelete = await _unitOfWork.Notes.GetNotesByTopicIdAsync(id);
            var questionsToDelete = await _unitOfWork.Questions.GetQuestionsByTopicIdAsync(id);


            //Delete answers related to questions then delete questions , notes and topic
            foreach (var question in questionsToDelete)
            {
                var answersToDelete = await _unitOfWork.Answers.GetAnswersByQuestionIdAsync(question.Id);
                _unitOfWork.Answers.RemoveRange(answersToDelete);
            }
            _unitOfWork.Notes.RemoveRange(notesToDelete);
            _unitOfWork.Questions.RemoveRange(questionsToDelete);


            _unitOfWork.Topics.Remove(topicToDelete);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<Topic> GetTopicByIdAsync(int id)
        {
            return await _unitOfWork.Topics.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Topic>> GetTopicBySubjectIdAsync(int subjectId)
        {
            return await _unitOfWork.Topics.GetTopicsBySubjectIdAsync(subjectId);
        }

        public async Task UpdateTopicAsync(int id, string newName)
        {
            if (id <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("A téma neve nem lehet üres.", nameof(newName));
            }

            if (newName.Length > 100)
            {
                throw new ArgumentException("A téma neve maximum 100 karakter hosszú lehet.", nameof(newName));
            }

            var topicToUpdate = await _unitOfWork.Topics.GetByIdAsync(id);

            if (topicToUpdate == null)
            {
                throw new ArgumentException("A megadott témakör nem található.", nameof(id));
            }

            topicToUpdate.Name = newName;

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
