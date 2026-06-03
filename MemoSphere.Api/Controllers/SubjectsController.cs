using Microsoft.AspNetCore.Mvc;
using Core.Interfaces.Services;
using Core.Entities;
using Core.Models;
using System.Linq;

namespace MemoSphere.Api.Controllers
{
    public class SubjectsController : ApiControllerBase
    {
        private readonly ISubjectService _subjectService;

        public SubjectsController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDetails = false)
        {
            if (includeDetails)
            {
                var detailedSubjects = await _subjectService.GetAllSubjectsWithTopicsAsync();

                var exportResult = detailedSubjects.Select(s => new SubjectExportDto
                {
                    Title = s.Title,
                    Topics = s.Topics.Select(t => new TopicExportDto
                    {
                        Title = t.Title,
                        Notes = t.Notes.Select(n => new NoteExportDto
                        {
                            Title = n.Title,
                            Content = n.Content,
                            Questions = n.Questions.Select(q => new QuestionExportDto
                            {
                                Text = q.Text,
                                QuestionType = q.QuestionType,

                                Answers = q.Answers.Select(a => new AnswerExportDto
                                {
                                    Text = a.Text,
                                    IsCorrect = a.IsCorrect
                                }).ToList()
                            }).ToList()
                        }).ToList()
                    }).ToList()
                }).ToList();

                return Ok(exportResult);
            }

            var subjects = await _subjectService.GetAllSubjectsAsync();
            return Ok(subjects);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null)
                return NotFound($"Tantárgy nem található a megadott azonosítóval: {id}");

            return Ok(subject);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("A tantárgy címe nem lehet üres.");

            try
            {
                var newSubject = await _subjectService.AddSubjectAsync(request.Title);
                return CreatedAtAction(nameof(GetById), new { id = newSubject.Id }, newSubject);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSubjectRequest request)
        {
            if (request == null || id != request.Id)
                return BadRequest("ID eltérés a kérésben.");

            try
            {
                var subjectToUpdate = new Subject
                {
                    Id = request.Id,
                    Title = request.Title
                };

                var updatedSubject = await _subjectService.UpdateSubjectAsync(subjectToUpdate);
                return Ok(updatedSubject);
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
                await _subjectService.DeleteSubjectAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}