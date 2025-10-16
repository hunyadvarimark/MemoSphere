using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface IQuizService
    {
        Task<List<Question>> GetRandomQuestionsForQuizAsync(List<int> topicIds, int count);
    }
}
