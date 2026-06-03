using Microsoft.AspNetCore.Mvc;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System;

namespace MemoSphere.Api.Controllers
{
    public class DataSharesController : ApiControllerBase
    {
        private readonly INoteShareService _noteShareService;

        public DataSharesController(INoteShareService noteShareService)
        {
            _noteShareService = noteShareService;
        }

        [HttpGet("export/note/{noteId:int}")]
        public async Task<IActionResult> ExportNote(int noteId)
        {
            string tempFilePath = Path.GetTempFileName();
            try
            {
                await _noteShareService.ExportNoteToFileAsync(noteId, tempFilePath);

                var bytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                return File(bytes, "application/json", $"note_{noteId}.json");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
            }
        }

        [HttpGet("export/topic/{topicId:int}")]
        public async Task<IActionResult> ExportTopic(int topicId)
        {
            string tempFilePath = Path.GetTempFileName();
            try
            {
                await _noteShareService.ExportTopicToFileAsync(topicId, tempFilePath);
                var bytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                return File(bytes, "application/json", $"topic_{topicId}.json");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
            }
        }

        [HttpGet("export/subject/{subjectId:int}")]
        public async Task<IActionResult> ExportSubject(int subjectId)
        {
            string tempFilePath = Path.GetTempFileName();
            try
            {
                await _noteShareService.ExportSubjectToFileAsync(subjectId, tempFilePath);
                var bytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                return File(bytes, "application/json", $"subject_{subjectId}.json");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
            }
        }

        [HttpPost("import/note")]
        public async Task<IActionResult> ImportNote(IFormFile file, [FromQuery] int targetTopicId)
        {
            if (file == null || file.Length == 0) return BadRequest("Nincs fájl feltöltve.");

            string tempFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                await _noteShareService.ImportNoteFromFileAsync(tempFilePath, targetTopicId);
                return Ok(new { Message = "Jegyzet és kérdései sikeresen importálva." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
            }
        }

        [HttpPost("import/topic")]
        public async Task<IActionResult> ImportTopic(IFormFile file, [FromQuery] int targetSubjectId)
        {
            if (file == null || file.Length == 0) return BadRequest("Nincs fájl feltöltve.");

            string tempFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                await _noteShareService.ImportTopicFromFileAsync(tempFilePath, targetSubjectId);
                return Ok(new { Message = "Témakör és összes beágyazott jegyzete sikeresen importálva." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
            }
        }

        [HttpPost("import/subject")]
        public async Task<IActionResult> ImportSubject(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Nincs fájl feltöltve.");

            string tempFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                await _noteShareService.ImportSubjectFromFileAsync(tempFilePath);
                return Ok(new { Message = "Tantárgy a teljes beágyazott struktúrájával együtt sikeresen importálva." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
            }
        }
    }
}