using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface IQuizService
    {
        Task<List<Question>> GetRandomQuestionsForQuizAsync(List<int> topicIds, int count);
        Task<int> GetQuestionCountForTopicsAsync(List<int> topicIds);

    }
}
