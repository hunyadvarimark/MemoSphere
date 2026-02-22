using Core.Entities;
using Core.Enums;

namespace Core.Interfaces.Services
{
    public interface IQuestionService
    {
        Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId);
        Task DeleteQuestionAsync(int id);
        Task<bool> GenerateAndSaveQuestionsAsync(int noteId, QuestionType type);
        Task<IEnumerable<Question>> GetQuestionsForNoteAsync(int noteId);
        Task<(bool IsCorrect, string Explanation)> EvaluateUserShortAnswerAsync(int questionId, string userAnswer);
        Task RecordAnswerAsync(int questionId, bool isCorrect);
        Task<List<Question>> GetWeightedQuestionsAsync(int topicId, int count, QuestionType? type = null);
        Task DeleteQuestionsForNoteAsync(int noteId);
        Task SaveQuestionsAsync(IEnumerable<Question> questions);
    }
}
