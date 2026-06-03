using Microsoft.AspNetCore.Mvc;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System;

namespace MemoSphere.Api.Controllers
{
    public class DocumentImportsController : ApiControllerBase
    {
        private readonly IDocumentImportService _documentImportService;

        public DocumentImportsController(IDocumentImportService documentImportService)
        {
            _documentImportService = documentImportService;
        }

        [HttpPost("extract-pdf")]
        public async Task<IActionResult> ExtractPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nincs fájl feltöltve vagy a fájl üres.");

            if (!_documentImportService.IsPdfFile(file.FileName))
                return BadRequest("A feltöltött fájl nem PDF formátumú.");

            string tempFilePath = Path.GetTempFileName();

            string tempPdfPath = Path.ChangeExtension(tempFilePath, ".pdf");

            try
            {
                using (var stream = new FileStream(tempPdfPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string formattedMarkdown = await _documentImportService.ExtractTextFromPdfAsync(tempPdfPath);

                return Ok(new { Content = formattedMarkdown });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Hiba a PDF feldolgozása során: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(tempPdfPath))
                    System.IO.File.Delete(tempPdfPath);

                if (System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
            }
        }
    }
}