using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyrlasAIEngine.Services;
using System.IO;
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
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не загружен.");

            using var stream = file.OpenReadStream();
            await _ragService.ProcessAndIndexFileAsync(stream, file.FileName); 
            // исправлено: теперь передаём Stream + FileName

            return Ok(new { message = $"Файл {file.FileName} успешно обработан и проиндексирован." });
        }
    }
}
