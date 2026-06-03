using Microsoft.AspNetCore.Mvc;
using Core.Interfaces.Services;
using Core.Entities;
using Core.Models;

namespace MemoSphere.Api.Controllers
{
    public class NotesController : ApiControllerBase
    {
        private readonly INoteService _noteService;

        public NotesController(INoteService noteService)
        {
            _noteService = noteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotesByTopic([FromQuery] int topicId)
        {
            if (topicId <= 0)
                return BadRequest("A téma azonosítója érvénytelen.");

            try
            {
                var notes = await _noteService.GetNotesByTopicIdAsync(topicId);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var note = await _noteService.GetNoteByIdAsync(id);
            if (note == null)
                return NotFound($"Jegyzet nem található: {id}");

            return Ok(note);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNoteRequest request)
        {
            if (request == null)
                return BadRequest("Üres kérés.");

            try
            {
                var noteEntity = new Note
                {
                    Title = request.Title,
                    Content = request.Content,
                    TopicId = request.TopicId
                };

                var newNote = await _noteService.AddNoteAsync(noteEntity);
                return CreatedAtAction(nameof(GetById), new { id = newNote.Id }, newNote);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateNoteRequest request)
        {
            if (request == null || id != request.Id)
                return BadRequest("ID eltérés.");

            try
            {
                var noteToUpdate = new Note
                {
                    Id = request.Id,
                    Title = request.Title,
                    Content = request.Content,
                    TopicId = request.TopicId
                };

                var updatedNote = await _noteService.UpdateNoteAsync(noteToUpdate);
                return Ok(updatedNote);
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
                await _noteService.DeleteNoteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}