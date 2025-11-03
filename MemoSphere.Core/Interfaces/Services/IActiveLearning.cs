using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface IActiveLearningService
    {
        Task<List<ActiveTopic>> GetActiveTopicsAsync();
        Task<ActiveTopic> GetActiveTopicAsync(int topicId);
        Task<bool> IsTopicActiveAsync(int topicId);
        Task ActivateTopicAsync(int topicId, int dailyGoal = 10);
        Task DeactivateTopicAsync(int topicId);
        Task UpdateProgressAsync(int topicId, bool isCorrect);
        Task CheckStreaksOnLoginAsync();
        Task<double> GetMasteryPercentageAsync(int topicId);
        Task<int> GetTodayQuestionsCountAsync(int topicId);
    }
}