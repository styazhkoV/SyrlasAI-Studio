using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyrlasAIEngine.Services;
using System;
using System.Threading.Tasks;

namespace SyrlasAIEngine.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RagController : ControllerBase
    {
        private readonly RagService _ragService;

        public RagController(RagService ragService)
        {
            _ragService = ragService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file, [FromQuery] string? sessionId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не передан или пуст.");

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _ragService.ProcessAndIndexFileAsync(stream, file.FileName, sessionId);
                return Ok(result);
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обработке документа: {ex.Message}");
            }
        }
    }
}