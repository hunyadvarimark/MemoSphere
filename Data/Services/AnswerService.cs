using Core.Interfaces;
using Core.Interfaces.Services;

namespace Data.Services
{
    public class AnswerService : IAnswerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnswerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> EvaluateAnswerAsync(int answerId)
        {
            var answer = await _unitOfWork.Answers.GetByIdAsync(answerId);

            if (answer == null)
            {
                // Kezeljük, ha a válasz azonosítója nem létezik az adatbázisban.
                return false;
            }
            if (answer.IsCorrect)
            {
                //statisztika frissités a jövőben
                return true;
            }

            return false;
        }
    }
}