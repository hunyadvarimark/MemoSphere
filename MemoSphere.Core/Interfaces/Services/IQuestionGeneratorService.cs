using Core.Enums;
using Core.Models;

namespace Core.Interfaces.Services
{
    public interface IQuestionGeneratorService
    {
        Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, QuestionType type, string modelNameOverride = null);
        Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelIdentifier);
    }

}