using Microsoft.AspNetCore.Mvc;
using Core.Interfaces.Services;
using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace MemoSphere.Api.Controllers
{
    public class QuizController : ApiControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly IActiveLearningService _activeLearningService;

        public QuizController(IQuizService quizService, IActiveLearningService activeLearningService)
        {
            _quizService = quizService;
            _activeLearningService = activeLearningService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQuiz([FromBody] List<int> topicIds, [FromQuery] int count = 10)
        {
            if (topicIds == null || !topicIds.Any())
                return BadRequest("Legalább egy témakört ki kell választani a kvíz indításához.");

            try
            {
                var questions = await _quizService.GetRandomQuestionsForQuizAsync(topicIds, count);
                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("count")]
        public async Task<IActionResult> GetQuestionCount([FromBody] List<int> topicIds)
        {
            try
            {
                int count = await _quizService.GetQuestionCountForTopicsAsync(topicIds);
                return Ok(new { TotalQuestions = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromQuery] int topicId, [FromQuery] bool isCorrect)
        {
            try
            {
                await _activeLearningService.UpdateProgressAsync(topicId, isCorrect);
                return Ok(new { Success = true, Message = "A tanulási előrehaladás sikeresen frissítve." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("active-topics")]
        public async Task<IActionResult> GetActiveTopics()
        {
            try
            {
                var activeTopics = await _activeLearningService.GetActiveTopicsAsync();
                return Ok(activeTopics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("activate")]
        public async Task<IActionResult> ActivateTopic([FromQuery] int topicId, [FromQuery] int dailyGoal = 10)
        {
            try
            {
                await _activeLearningService.ActivateTopicAsync(topicId, dailyGoal);
                return Ok(new { Success = true, Message = "Témakör sikeresen aktiválva a napi célhoz." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("deactivate/{topicId:int}")]
        public async Task<IActionResult> DeactivateTopic(int topicId)
        {
            try
            {
                await _activeLearningService.DeactivateTopicAsync(topicId);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("check-streaks")]
        public async Task<IActionResult> CheckLoginStreaks()
        {
            try
            {
                await _activeLearningService.CheckStreaksOnLoginAsync();
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("topic-stats/{topicId:int}")]
        public async Task<IActionResult> GetTopicStats(int topicId)
        {
            try
            {
                var mastery = await _activeLearningService.GetMasteryPercentageAsync(topicId);
                var todayCount = await _activeLearningService.GetTodayQuestionsCountAsync(topicId);

                return Ok(new
                {
                    TopicId = topicId,
                    MasteryPercentage = mastery,
                    TodayQuestionsAnswered = todayCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}