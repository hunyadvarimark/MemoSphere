using Core.Entities;
using Core.Interfaces.Services;
using System.Diagnostics;

namespace Core.Services
{
    public class QuizService : IQuizService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly IQuestionService _questionService;

        public QuizService(
            IUnitOfWork unitOfWork,
            IAuthService authService,
            IQuestionService questionService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _authService = authService;
            _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
        }

        public async Task<List<Question>> GetRandomQuestionsForQuizAsync(List<int> topicIds, int count)
        {
            var userId = _authService.GetCurrentUserId();

            Debug.WriteLine($"🔍 GetRandomQuestionsForQuizAsync called (WEIGHTED VERSION)");
            Debug.WriteLine($"   - UserId: {userId}");
            Debug.WriteLine($"   - TopicIds: {string.Join(", ", topicIds)}");
            Debug.WriteLine($"   - Requested count: {count}");

            if (topicIds == null || !topicIds.Any() || count <= 0)
            {
                Debug.WriteLine("⚠️ Invalid parameters");
                return new List<Question>();
            }

            var validTopicIds = topicIds.Take(3).ToList();
            var allSelectedQuestions = new List<Question>();

            // Minden témából arányosan kérünk kérdéseket
            int questionsPerTopic = Math.Max(1, count / validTopicIds.Count);
            int remainder = count % validTopicIds.Count;

            for (int i = 0; i < validTopicIds.Count; i++)
            {
                int topicId = validTopicIds[i];
                int questionsToFetch = questionsPerTopic + (i < remainder ? 1 : 0);

                Debug.WriteLine($"📚 Fetching {questionsToFetch} weighted questions from Topic {topicId}");

                // ✅ SÚLYOZOTT KÉRDÉSEK LEKÉRÉSE
                var weightedQuestions = await _questionService.GetWeightedQuestionsAsync(
                    topicId: topicId,
                    count: questionsToFetch,
                    type: null // Minden típus
                );

                allSelectedQuestions.AddRange(weightedQuestions);
                Debug.WriteLine($"   ✓ Got {weightedQuestions.Count} questions");
            }

            // Ha kevesebb jött vissza, mint amennyit kértünk, kiegészítjük a maradékkal
            if (allSelectedQuestions.Count < count)
            {
                int missing = count - allSelectedQuestions.Count;
                Debug.WriteLine($"⚠️ Missing {missing} questions, fetching more...");

                foreach (var topicId in validTopicIds)
                {
                    var extraQuestions = await _questionService.GetWeightedQuestionsAsync(
                        topicId: topicId,
                        count: missing,
                        type: null
                    );

                    // Csak azokat adjuk hozzá, amelyek még nincsenek benne
                    var newQuestions = extraQuestions
                        .Where(q => !allSelectedQuestions.Any(existing => existing.Id == q.Id))
                        .Take(missing)
                        .ToList();

                    allSelectedQuestions.AddRange(newQuestions);
                    missing -= newQuestions.Count;

                    if (missing <= 0) break;
                }
            }

            // Véletlenszerű sorrendbe rakjuk (hogy ne mindig ugyanabban a sorrendben jöjjenek)
            var random = new Random();
            var shuffledQuestions = allSelectedQuestions
                .OrderBy(q => random.Next())
                .Take(count)
                .ToList();

            Debug.WriteLine($"✅ Returning {shuffledQuestions.Count} weighted questions");

            return shuffledQuestions;
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
                var topicFilterCount = await _unitOfWork.Questions.CountAsync(
                    q => topicIds.Contains(q.TopicId) && q.IsActive
                );
                Debug.WriteLine($"⚠️ Falling back to count without UserId check: {topicFilterCount}");
                finalCount = topicFilterCount;
            }

            Debug.WriteLine($"✅ Returning count: {finalCount}");
            Debug.WriteLine($"════════════════════════════════════════");

            return finalCount;
        }
    }
}