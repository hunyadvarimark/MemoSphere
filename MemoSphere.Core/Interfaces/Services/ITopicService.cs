using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface ITopicService
    {
        Task<Topic> GetTopicByIdAsync(int id);
        Task<IEnumerable<Topic>> GetTopicBySubjectIdAsync(int subjectId);
        Task<IEnumerable<Topic>> GetTopicsWithNotesAndQuestionsAsync();
        Task<Topic> GetTopicWithNotesAndQuestionsAsync(int id);

        Task<Topic> AddTopicAsync(Topic topic);
        Task DeleteTopicAsync(int id);
        Task<Topic> UpdateTopicAsync(Topic topic);
        Task<bool> TopicExistsAsync(string title, int subjectId, int? excludeId = null);
    }
}