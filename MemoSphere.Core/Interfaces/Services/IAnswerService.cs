namespace Core.Interfaces.Services
{
    public interface IAnswerService
    {
        Task<bool> EvaluateAnswerAsync(int answerId);
    }
}