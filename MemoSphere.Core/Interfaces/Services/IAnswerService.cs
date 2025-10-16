using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface IAnswerService
    {
        Task<bool> EvaluateAnswerAsync(int answerId);
        Task<IEnumerable<Answer>> GetAnswersByQuestionIdAsync(int questionId);
    }
}