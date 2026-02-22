using Core.Entities;
using Core.Interfaces.Services;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Data.Services
{
    public class ActiveLearningService : IActiveLearningService
    {
        private readonly IDbContextFactory<MemoSphereDbContext> _factory;
        private readonly IAuthService _authService;
        public ActiveLearningService(
            IDbContextFactory<MemoSphereDbContext> factory,
            IAuthService authService)
        {
            _factory = factory;
            _authService = authService;
        }
        public async Task<List<ActiveTopic>> GetActiveTopicsAsync()
        {
            var userId = _authService.GetCurrentUserId();
            using var context = _factory.CreateDbContext();
            return await context.ActiveTopics
                .Include(at => at.Topic)
                .Where(at => at.UserId == userId && at.IsActive)
                .OrderByDescending(at => at.ActivatedAt)
                .ToListAsync();
        }
        public async Task<ActiveTopic> GetActiveTopicAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();
            using var context = _factory.CreateDbContext();
            return await context.ActiveTopics
                .Include(at => at.Topic)
                .FirstOrDefaultAsync(at => at.UserId == userId && at.TopicId == topicId && at.IsActive);
        }
        public async Task<bool> IsTopicActiveAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();
            using var context = _factory.CreateDbContext();
            return await context.ActiveTopics
                .AnyAsync(at => at.UserId == userId && at.TopicId == topicId && at.IsActive);
        }
        public async Task ActivateTopicAsync(int topicId, int dailyGoal = 10)
        {
            var userId = _authService.GetCurrentUserId();
            using var context = _factory.CreateDbContext();
            var existing = await context.ActiveTopics
                .FirstOrDefaultAsync(at => at.UserId == userId && at.TopicId == topicId);
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
                context.ActiveTopics.Add(newActiveTopic);
            }
            await context.SaveChangesAsync();
        }
        public async Task DeactivateTopicAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();
            using var context = _factory.CreateDbContext();
            var activeTopic = await context.ActiveTopics
                .FirstOrDefaultAsync(at => at.UserId == userId && at.TopicId == topicId);
            if (activeTopic != null)
            {
                activeTopic.IsActive = false;
                await context.SaveChangesAsync();
            }
        }
        public async Task UpdateProgressAsync(int topicId, bool isCorrect)
        {
            var userId = _authService.GetCurrentUserId();
            using var context = _factory.CreateDbContext();
            var today = DateTime.Today.ToUniversalTime().Date;
            var activeTopic = await context.ActiveTopics
                .FirstOrDefaultAsync(at => at.UserId == userId && at.TopicId == topicId && at.IsActive);
            if (activeTopic == null) return;
            activeTopic.LastPracticedAt = DateTime.UtcNow;
            var dailyProgress = await context.DailyProgresses
                .FirstOrDefaultAsync(dp => dp.UserId == userId && dp.TopicId == topicId && dp.Date == today);
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
                context.DailyProgresses.Add(dailyProgress);
            }
            if (isCorrect)
            {
                dailyProgress.QuestionsAnswered++;
            }
            bool goalJustMet = !dailyProgress.GoalReached && (dailyProgress.QuestionsAnswered >= dailyProgress.GoalQuestions);
            if (goalJustMet)
            {
                dailyProgress.GoalReached = true;


                var yesterdayGoalMet = await context.DailyProgresses
                    .AnyAsync(dp => dp.UserId == userId && dp.TopicId == topicId &&
                                   dp.Date == today.AddDays(-1) && dp.GoalReached);
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
            await context.SaveChangesAsync();
        }
        public async Task CheckStreaksOnLoginAsync()
        {
            var userId = _authService.GetCurrentUserId();
            if (userId == Guid.Empty) return;

            using var context = _factory.CreateDbContext();
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var activeTopics = await context.ActiveTopics
                .Where(at => at.UserId == userId && at.IsActive && at.CurrentStreak > 0)
                .ToListAsync();

            if (!activeTopics.Any()) return;
            
            var relevantTopicIds = activeTopics.Select(at => at.TopicId).ToList();
            var recentProgress = await context.DailyProgresses
                .Where(dp => dp.UserId == userId && relevantTopicIds.Contains(dp.TopicId) && dp.GoalReached)
                .OrderByDescending(dp => dp.Date)
                .ToListAsync();

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
                    needsSave = true;
                    continue;
                }
                if (lastGoalMetDate.Date != today && lastGoalMetDate.Date != yesterday)
                {
                    topic.CurrentStreak = 0;
                    needsSave = true;
                }
            }
            if (needsSave)
            {
                await context.SaveChangesAsync();
            }
        }
        public async Task<double> GetMasteryPercentageAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();
            using var context = _factory.CreateDbContext();
            var allQuestionIdsInTopic = await context.Questions
                .Where(q => q.TopicId == topicId && q.UserId == userId && q.IsActive)
                .Select(q => q.Id)
                .ToListAsync();
            if (allQuestionIdsInTopic.Count == 0) return 0.0;
            var statistics = await context.QuestionStatistics
                .Where(qs => qs.UserId == userId && allQuestionIdsInTopic.Contains(qs.QuestionId))
                .ToListAsync();
            int knownQuestions = statistics.Count(qs => qs.TimesCorrect > 0);
            return ((double)knownQuestions / allQuestionIdsInTopic.Count) * 100.0;
        }
        public async Task<int> GetTodayQuestionsCountAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();
            var today = DateTime.UtcNow.Date;
            using var context = _factory.CreateDbContext();
            var dailyProgress = await context.DailyProgresses
                .FirstOrDefaultAsync(dp => dp.UserId == userId && dp.TopicId == topicId && dp.Date == today);
            return dailyProgress?.QuestionsAnswered ?? 0;
        }

    }
}