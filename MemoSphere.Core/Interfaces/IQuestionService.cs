using Core.Entities;

namespace Core.Interfaces
{
    public interface IQuestionService
    {
        Task<bool>GenerateAndSaveQuestionsAsync(int noteId);
        Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId);
        Task DeleteQuestionAsync(int id);
    }
}
