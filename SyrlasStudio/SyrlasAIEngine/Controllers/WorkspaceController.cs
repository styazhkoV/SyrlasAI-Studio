using Microsoft.AspNetCore.Mvc;
using SyrlasAIEngine.Services;
using System.Threading.Tasks;

namespace SyrlasAIEngine.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkspaceController : ControllerBase
    {
        private readonly AgentService _agentService;

        public WorkspaceController(AgentService agentService)
        {
            _agentService = agentService;
        }

        public class SaveArtifactRequest
        {
            public required string Title { get; set; }
            public required string Content { get; set; }
            public required string Type { get; set; }
        }


        [HttpPost("artifact")]
        public async Task<IActionResult> SaveArtifact([FromBody] SaveArtifactRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || 
                string.IsNullOrWhiteSpace(request.Content) || 
                string.IsNullOrWhiteSpace(request.Type))
            {
                return BadRequest("Title, Content и Type обязательны.");
            }

            await _agentService.SaveArtifactAsync(request.Title, request.Content, request.Type);
            return Ok(new { message = "Артефакт сохранён" });
        }
    }
}
