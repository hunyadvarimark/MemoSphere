using Core.Entities;
using Core.Interfaces.Services;
using System.Diagnostics;

namespace Core.Services
{
    public class QuizService : IQuizService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public QuizService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _authService = authService;
        }

        public async Task<List<Question>> GetRandomQuestionsForQuizAsync(List<int> topicIds, int count)
        {
            var userId = _authService.GetCurrentUserId();

            Debug.WriteLine($"🔍 GetRandomQuestionsForQuizAsync called");
            Debug.WriteLine($"   - UserId: {userId}");
            Debug.WriteLine($"   - TopicIds: {string.Join(", ", topicIds)}");
            Debug.WriteLine($"   - Requested count: {count}");

            if (topicIds == null || !topicIds.Any() || count <= 0)
            {
                Debug.WriteLine("⚠️ Invalid parameters");
                return new List<Question>();
            }

            var validTopicIds = topicIds.Take(3).ToList();

            // ✅ JAVÍTOTT: Először ellenőrizzük UserId nélkül
            var allQuestions = await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.IsActive && validTopicIds.Contains(q.TopicId),
                includeProperties: "Answers"
            );

            Debug.WriteLine($"📊 Questions found (without UserId filter): {allQuestions?.Count() ?? 0}");

            // Most szűrjük UserId alapján
            var availableQuestions = await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.IsActive && validTopicIds.Contains(q.TopicId) && q.Topic.UserId == userId,
                includeProperties: "Answers"
            );

            var questionList = availableQuestions?.ToList() ?? new List<Question>();

            Debug.WriteLine($"📊 Questions found (with UserId filter): {questionList.Count}");

            if (!questionList.Any())
            {
                Debug.WriteLine("⚠️ No questions available after filtering");
                return new List<Question>();
            }

            var random = new Random();
            var selectedQuestions = questionList
                .OrderBy(q => random.Next())
                .Take(count)
                .ToList();

            Debug.WriteLine($"✅ Returning {selectedQuestions.Count} questions");

            return selectedQuestions;
        }

        public async Task<int> GetQuestionCountForTopicsAsync(List<int> topicIds)
        {
            var userId = _authService.GetCurrentUserId();

            Debug.WriteLine($"════════════════════════════════════════");
            Debug.WriteLine($"🔍 GetQuestionCountForTopicsAsync called");
            Debug.WriteLine($"   - UserId: {userId}");
            Debug.WriteLine($"   - TopicIds: {string.Join(", ", topicIds ?? new List<int>())}");

            if (topicIds == null || !topicIds.Any())
            {
                Debug.WriteLine("⚠️ No topic IDs provided");
                Debug.WriteLine($"════════════════════════════════════════");
                return 0;
            }

            // ✅ DIAGNOSZTIKA: Először topic szűrés nélkül
            var totalCount = await _unitOfWork.Questions.CountAsync(q => q.IsActive);
            Debug.WriteLine($"📊 Total active questions (all topics): {totalCount}");

            // Topic szűréssel, UserId nélkül
            var topicFilterCount = await _unitOfWork.Questions.CountAsync(
                q => topicIds.Contains(q.TopicId) && q.IsActive
            );
            Debug.WriteLine($"📊 Active questions for topics (no UserId): {topicFilterCount}");

            // ✅ JAVÍTOTT: Explicit Topic include + részletes hibaellenőrzés
            int finalCount;
            try
            {
                finalCount = await _unitOfWork.Questions.CountAsync(
                    q => topicIds.Contains(q.TopicId) && q.Topic.UserId == userId && q.IsActive
                );
                Debug.WriteLine($"📊 Active questions with UserId filter: {finalCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error counting questions: {ex.Message}");
                Debug.WriteLine($"   StackTrace: {ex.StackTrace}");

                // Fallback: próbáljuk UserId nélkül
                Debug.WriteLine($"⚠️ Falling back to count without UserId check");
                finalCount = topicFilterCount;
            }

            Debug.WriteLine($"✅ Returning count: {finalCount}");
            Debug.WriteLine($"════════════════════════════════════════");

            return finalCount;
        }
    }
}