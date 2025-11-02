using Core.Enums;
using Core.Models;

namespace Core.Interfaces.Services
{
    public interface IQuestionGeneratorService
    {
        Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, QuestionType type, string modelNameOverride = null);
        Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelNameOverride = null);
        Task<bool> EvaluateAnswerAsync(string questionText, string userAnswer, string correctAnswer, string modelNameOverride = null);
        Task<string> CleanupAndFormatNoteAsync(string rawText, string modelNameOverride = null);
    }

}