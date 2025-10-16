public interface IProgressService
{
    Task<double> GetSuccessRateAsync(int topicId);
    Task UpdateProgressAsync(int questionId, bool isCorrect);
}