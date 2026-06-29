using Core.Entities;
using Core.Interfaces.Services;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Data.Services
{
    public class ActiveLearningService : IActiveLearningService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public ActiveLearningService(
            IUnitOfWork unitOfWork,
            IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<List<ActiveTopic>> GetActiveTopicsAsync()
        {
            var userId = _authService.GetCurrentUserId();
            var activeTopics = await _unitOfWork.ActiveTopics.GetFilteredAsync(
                filter: at => at.UserId == userId && at.IsActive,
                includeProperties: "Topic",
                orderBy: q => q.OrderByDescending(at => at.ActivatedAt)
            );
            return activeTopics.ToList();
        }

        public async Task<ActiveTopic> GetActiveTopicAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();
            var activeTopics = await _unitOfWork.ActiveTopics.GetFilteredAsync(
                filter: at => at.UserId == userId && at.TopicId == topicId && at.IsActive,
                includeProperties: "Topic"
            );
            return activeTopics.FirstOrDefault();
        }

        public async Task<bool> IsTopicActiveAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();
            return await _unitOfWork.ActiveTopics.ExistsAsync(
                at => at.UserId == userId && at.TopicId == topicId && at.IsActive
            );
        }

        public async Task ActivateTopicAsync(int topicId, int dailyGoal = 10)
        {
            var userId = _authService.GetCurrentUserId();

            var activeTopics = await _unitOfWork.ActiveTopics.GetFilteredAsync(
                filter: at => at.UserId == userId && at.TopicId == topicId
            );
            var existing = activeTopics.FirstOrDefault();

            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    existing.ActivatedAt = DateTime.UtcNow;
                    existing.DailyGoalQuestions = dailyGoal;
                    existing.CurrentStreak = 0;
                    existing.LastPracticedAt = null;
                }
                else
                {
                    existing.DailyGoalQuestions = dailyGoal;
                }
                _unitOfWork.ActiveTopics.Update(existing);
            }
            else
            {
                var newActiveTopic = new ActiveTopic
                {
                    UserId = userId,
                    TopicId = topicId,
                    DailyGoalQuestions = dailyGoal,
                    ActivatedAt = DateTime.UtcNow,
                    IsActive = true,
                    CurrentStreak = 0,
                    LongestStreak = 0
                };
                await _unitOfWork.ActiveTopics.AddAsync(newActiveTopic);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeactivateTopicAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();

            var activeTopics = await _unitOfWork.ActiveTopics.GetFilteredAsync(
                filter: at => at.UserId == userId && at.TopicId == topicId
            );
            var activeTopic = activeTopics.FirstOrDefault();

            if (activeTopic != null)
            {
                activeTopic.IsActive = false;
                _unitOfWork.ActiveTopics.Update(activeTopic);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task UpdateProgressAsync(int topicId, bool isCorrect)
        {
            var userId = _authService.GetCurrentUserId();

            var today = DateTime.Today.ToUniversalTime().Date;
            var activeTopics = await _unitOfWork.ActiveTopics.GetFilteredAsync(
                filter: at => at.UserId == userId && at.TopicId == topicId && at.IsActive
            );
            var activeTopic = activeTopics.FirstOrDefault();

            if (activeTopic == null) return;

            activeTopic.LastPracticedAt = DateTime.UtcNow;

            var progresses = await _unitOfWork.DailyProgresses.GetFilteredAsync(
                filter: dp => dp.UserId == userId && dp.TopicId == topicId && dp.Date == today
            );
            var dailyProgress = progresses.FirstOrDefault();

            if (dailyProgress == null)
            {
                dailyProgress = new DailyProgress
                {
                    UserId = userId,
                    TopicId = topicId,
                    Date = today,
                    QuestionsAnswered = 0,
                    GoalQuestions = activeTopic.DailyGoalQuestions,
                    GoalReached = false
                };
                await _unitOfWork.DailyProgresses.AddAsync(dailyProgress);
            }
            else
            {
                _unitOfWork.DailyProgresses.Update(dailyProgress);
            }

            if (isCorrect)
            {
                dailyProgress.QuestionsAnswered++;
            }

            bool goalJustMet = !dailyProgress.GoalReached && (dailyProgress.QuestionsAnswered >= dailyProgress.GoalQuestions);

            if (goalJustMet)
            {
                dailyProgress.GoalReached = true;

                var yesterdayGoalMet = await _unitOfWork.DailyProgresses.ExistsAsync(
                    dp => dp.UserId == userId && dp.TopicId == topicId &&
                                   dp.Date == today.AddDays(-1) && dp.GoalReached
                );
                if (yesterdayGoalMet)
                {
                    activeTopic.CurrentStreak++;
                }
                else
                {
                    activeTopic.CurrentStreak = 1;
                }
                if (activeTopic.CurrentStreak > activeTopic.LongestStreak)
                {
                    activeTopic.LongestStreak = activeTopic.CurrentStreak;
                }
            }

            _unitOfWork.ActiveTopics.Update(activeTopic);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CheckStreaksOnLoginAsync()
        {
            var userId = _authService.GetCurrentUserId();
            if (userId == Guid.Empty) return;

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var activeTopicsIterable = await _unitOfWork.ActiveTopics.GetFilteredAsync(
                filter: at => at.UserId == userId && at.IsActive && at.CurrentStreak > 0
            );
            var activeTopics = activeTopicsIterable.ToList();

            if (!activeTopics.Any()) return;

            var relevantTopicIds = activeTopics.Select(at => at.TopicId).ToList();

            var recentProgressIterable = await _unitOfWork.DailyProgresses.GetFilteredAsync(
                filter: dp => dp.UserId == userId && relevantTopicIds.Contains(dp.TopicId) && dp.GoalReached,
                orderBy: q => q.OrderByDescending(dp => dp.Date)
            );
            var recentProgress = recentProgressIterable.ToList();

            bool needsSave = false;

            foreach (var topic in activeTopics)
            {
                var lastGoalMetDate = recentProgress
                    .Where(dp => dp.TopicId == topic.TopicId)
                    .Select(dp => dp.Date)
                    .FirstOrDefault();

                if (lastGoalMetDate == default(DateTime))
                {
                    topic.CurrentStreak = 0;
                    _unitOfWork.ActiveTopics.Update(topic);
                    needsSave = true;
                    continue;
                }
                if (lastGoalMetDate.Date != today && lastGoalMetDate.Date != yesterday)
                {
                    topic.CurrentStreak = 0;
                    _unitOfWork.ActiveTopics.Update(topic);
                    needsSave = true;
                }
            }
            if (needsSave)
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<double> GetMasteryPercentageAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();

            var questionsIterable = await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.TopicId == topicId && q.UserId == userId && q.IsActive
            );
            var allQuestionIdsInTopic = questionsIterable.Select(q => q.Id).ToList();
            if (allQuestionIdsInTopic.Count == 0) return 0.0;

            var statistics = await _unitOfWork.QuestionStatistics.GetFilteredAsync(
                filter: qs => qs.UserId == userId && allQuestionIdsInTopic.Contains(qs.QuestionId)
            );

            int knownQuestions = statistics.Count(qs => qs.TimesCorrect > 0);

            return ((double)knownQuestions / allQuestionIdsInTopic.Count) * 100.0;
        }

        public async Task<int> GetTodayQuestionsCountAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();
            var today = DateTime.UtcNow.Date;

            var progresses = await _unitOfWork.DailyProgresses.GetFilteredAsync(
                filter: dp => dp.UserId == userId && dp.TopicId == topicId && dp.Date == today
            );
            var dailyProgress = progresses.FirstOrDefault();

            return dailyProgress?.QuestionsAnswered ?? 0;
        }
    }
}