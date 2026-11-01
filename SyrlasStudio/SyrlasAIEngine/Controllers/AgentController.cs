using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyrlasAIEngine.Models;
using SyrlasAIEngine.Services;

namespace SyrlasAIEngine.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly LlamaInferenceService _inferenceService;

        public AgentController(LlamaInferenceService inferenceService)
        {
            _inferenceService = inferenceService;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<AgentResponse>> Chat(
            [FromBody] AgentRequest request, 
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { error = "Промпт не может быть пустым." });
            }

            string aiResponse = await _inferenceService.GenerateResponseAsync(request.Prompt, cancellationToken);
            return Ok(new AgentResponse(aiResponse));
        }

        [HttpPost("stream")]
        public async Task StreamAgentResponse([FromBody] AgentRequestDto request, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/plain; charset=utf-8";

            await foreach (var token in _inferenceService.GenerateStreamAsync(request.Prompt, cancellationToken))
            {
                await Response.WriteAsync(token, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }
}