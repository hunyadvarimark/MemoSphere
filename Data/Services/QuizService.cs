using Core.Entities;
using Core.Interfaces.Services;

namespace Core.Services
{
    public class QuizService : IQuizService
    {
        private readonly IUnitOfWork _unitOfWork;
        public QuizService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<Question>> GetRandomQuestionsForQuizAsync(List<int> topicIds, int count)
        {
            // 0. Validáció
            if (topicIds == null || !topicIds.Any() || count <= 0)
            {
                return new List<Question>();
            }

            // Limitáljuk a bemeneti listát (Üzleti szabály: max. 3 témakör)
            var validTopicIds = topicIds.Take(3).ToList();

            var availableQuestions = await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.IsActive && validTopicIds.Contains(q.TopicId),
                includeProperties: "Answers"
            );

            var questionList = availableQuestions?.ToList() ?? new List<Question>();  // Null-safe

            if (!questionList.Any())
            {
                return new List<Question>();
            }

            // 2. Randomizáció (Üzleti Logika)
            var random = new Random();

            var selectedQuestions = questionList
                .OrderBy(q => random.Next()) // Keverés
                .Take(count)                 // Darabszám levágása
                .ToList();

            return selectedQuestions;
        }
    }
}