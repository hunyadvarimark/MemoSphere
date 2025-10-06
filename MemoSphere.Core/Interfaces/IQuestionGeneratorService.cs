using Core.Models;

namespace Core.Interfaces
{
    public interface IQuestionGeneratorService
    {
        Task<List<QuestionAnswerPair>> GenerateQuestionsAsync(string context, string modelIdentifier);
        Task<List<string>> GenerateWrongAnswersAsync(string correctAnswer, string context, string modelIdentifier);
    }

}