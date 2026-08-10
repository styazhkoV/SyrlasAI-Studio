using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyrlasAIEngine.Services;

namespace SyrlasAIEngine.Controllers
{
    public record AgentRequestDto(
        AgentRole Role, 
        string Prompt, 
        bool UseRag = true
    );

    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly AgentService _agentService;

        public AgentController(AgentService agentService)
        {
            _agentService = agentService;
        }

        [HttpPost("stream")]
        public async Task StreamAgentResponse([FromBody] AgentRequestDto request, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/plain; charset=utf-8";

            await foreach (var token in _agentService.ExecuteTaskAsync(
                request.Role, 
                request.Prompt, 
                request.UseRag, 
                cancellationToken))
            {
                await Response.WriteAsync(token, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }
}