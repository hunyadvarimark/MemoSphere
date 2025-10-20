using Core.Entities;
using Core.Interfaces;
using Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Data.Context;

namespace Data.Services
{
    public class AnswerService : IAnswerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public AnswerService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<bool> EvaluateAnswerAsync(int answerId)
        {
            var userId = _authService.GetCurrentUserId();

            var answer = await _unitOfWork.Answers.GetByIdAsync(answerId);

            if (answer == null || answer.Question.Topic.UserId != userId)
            {
                return false;
            }

            if (answer.IsCorrect)
            {
                //statisztika frissítés a jövőben
                return true;
            }

            return false;
        }

        public async Task<IEnumerable<Answer>> GetAnswersByQuestionIdAsync(int questionId)
        {
            var userId = _authService.GetCurrentUserId();

            var answers = await _unitOfWork.Answers.FindAsync(a => a.QuestionId == questionId && a.Question.Topic.UserId == userId);

            return answers ?? Enumerable.Empty<Answer>();
        }
    }
}