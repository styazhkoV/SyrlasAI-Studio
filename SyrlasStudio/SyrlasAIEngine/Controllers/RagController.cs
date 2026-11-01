using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyrlasAIEngine.Services;

namespace SyrlasAIEngine.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RagController : ControllerBase
    {
        private readonly RagService _ragService;
        private readonly ILogger<RagController> _logger;

        public RagController(RagService ragService, ILogger<RagController> logger)
        {
            _ragService = ragService;
            _logger = logger;
        }

        /// <summary>
        /// Загрузка и индексация документа (PDF, TXT, MD)
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file, [FromQuery] string? category = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Файл не передан или пуст." });

            try
            {
                using var stream = file.OpenReadStream();
                await _ragService.ProcessAndIndexFileAsync(stream, file.FileName, category);
                
                _logger.LogInformation("Файл {FileName} успешно проиндексирован.", file.FileName);
                return Ok(new { message = $"Файл '{file.FileName}' успешно загружен и проиндексирован." });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке файла {FileName}", file.FileName);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера при обработке файла." });
            }
        }

        /// <summary>
        /// Поиск релевантного контекста по запросу пользователя
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchContext([FromQuery] string query, [FromQuery] int limit = 3, [FromQuery] float threshold = 0.6f)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { error = "Поисковый запрос не может быть пустым." });

            try
            {
                var context = await _ragService.SearchContextAsync(query, limit, threshold);
                return Ok(new { query, context });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении поиска для запроса: {Query}", query);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера при поиске." });
            }
        }
    }
}