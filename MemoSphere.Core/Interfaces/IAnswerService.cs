using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IAnswerService
    {
        Task<bool> EvaluateAnswerAsync(int answerId);
    }
}