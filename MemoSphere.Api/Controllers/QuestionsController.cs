using Microsoft.AspNetCore.Mvc;
using Core.Interfaces.Services;
using Core.Entities;
using Core.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace MemoSphere.Api.Controllers
{
    public class QuestionsController : ApiControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet("by-topic/{topicId:int}")]
        public async Task<IActionResult> GetByTopic(int topicId)
        {
            try
            {
                var questions = await _questionService.GetQuestionsByTopicIdAsync(topicId);
                return Ok(questions);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("for-note/{noteId:int}")]
        public async Task<IActionResult> GetForNote(int noteId)
        {
            try
            {
                var questions = await _questionService.GetQuestionsForNoteAsync(noteId);
                return Ok(questions);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpPost("generate-ai")]
        public async Task<IActionResult> GenerateAndSaveQuestions([FromQuery] int noteId, [FromQuery] QuestionType type)
        {
            try
            {
                var success = await _questionService.GenerateAndSaveQuestionsAsync(noteId, type);
                if (!success)
                {
                    return BadRequest("Nem sikerült kérdéseket generálni a megadott jegyzethez (esetleg nincsenek chunkok).");
                }
                return Ok(new { Message = "A kérdések generálása és mentése sikeresen megtörtént." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("evaluate-short-answer")]
        public async Task<IActionResult> EvaluateShortAnswer([FromQuery] int questionId, [FromBody] string userAnswer)
        {
            try
            {
                var answerText = userAnswer ?? string.Empty;
                var result = await _questionService.EvaluateUserShortAnswerAsync(questionId, answerText);
                return Ok(new { IsCorrect = result.IsCorrect, Explanation = result.Explanation });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpPost("record-answer")]
        public async Task<IActionResult> RecordAnswer([FromQuery] int questionId, [FromQuery] bool isCorrect)
        {
            try
            {
                await _questionService.RecordAnswerAsync(questionId, isCorrect);
                return Ok(new { Message = "A válasz statisztikája sikeresen rögzítve." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("weighted")]
        public async Task<IActionResult> GetWeightedQuestions([FromQuery] int topicId, [FromQuery] int count, [FromQuery] QuestionType? type = null)
        {
            try
            {
                var questions = await _questionService.GetWeightedQuestionsAsync(topicId, count, type);
                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("save-batch")]
        public async Task<IActionResult> SaveQuestions([FromBody] List<Question> questions)
        {
            if (questions == null)
                return BadRequest("A küldött kérdéslista nem lehet üres.");

            try
            {
                await _questionService.SaveQuestionsAsync(questions);
                return Ok(new { Message = "A kérdések és válaszok mentése/frissítése sikeresen megtörtént." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _questionService.DeleteQuestionAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("for-note/{noteId:int}")]
        public async Task<IActionResult> DeleteForNote(int noteId)
        {
            try
            {
                await _questionService.DeleteQuestionsForNoteAsync(noteId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}