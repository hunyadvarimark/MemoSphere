using Microsoft.AspNetCore.Mvc;
using Core.Interfaces.Services;
using Core.Entities;
using Core.Models;

namespace MemoSphere.Api.Controllers
{
    public class TopicsController : ApiControllerBase
    {
        private readonly ITopicService _topicService;

        public TopicsController(ITopicService topicService)
        {
            _topicService = topicService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTopics([FromQuery] int? subjectId, [FromQuery] bool includeDetails = false)
        {
            if (includeDetails)
            {
                var detailedTopics = await _topicService.GetTopicsWithNotesAndQuestionsAsync();

                var exportResult = detailedTopics.Select(t => new TopicExportDto
                {
                    Title = t.Title,
                    Notes = t.Notes.Select(n => new NoteExportDto
                    {
                        Title = n.Title,
                        Content = n.Content,
                        Questions = n.Questions.Select(q => new QuestionExportDto
                        {
                            Text = q.Text,
                            QuestionType = q.QuestionType
                        }).ToList()
                    }).ToList()
                }).ToList();

                return Ok(exportResult);
            }

            if (subjectId.HasValue)
            {
                var topicsBySubject = await _topicService.GetTopicBySubjectIdAsync(subjectId.Value);
                return Ok(topicsBySubject);
            }

            var allTopics = await _topicService.GetTopicsWithNotesAndQuestionsAsync();
            return Ok(allTopics);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var topic = await _topicService.GetTopicByIdAsync(id);
            if (topic == null)
                return NotFound($"Témakör nem található: {id}");

            return Ok(topic);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTopicRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("A téma neve nem lehet üres.");

            try
            {
                var topicEntity = new Topic
                {
                    Title = request.Title,
                    SubjectId = request.SubjectId
                };

                var newTopic = await _topicService.AddTopicAsync(topicEntity);
                return CreatedAtAction(nameof(GetById), new { id = newTopic.Id }, newTopic);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Topic topicInput)
        {
            if (topicInput == null || id != topicInput.Id)
                return BadRequest("ID eltérés.");

            try
            {
                var updatedTopic = await _topicService.UpdateTopicAsync(topicInput);
                return Ok(updatedTopic);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _topicService.DeleteTopicAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}