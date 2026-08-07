using Microsoft.AspNetCore.Mvc;
using SyrlasAIEngine.Models;
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

        [HttpPost("artifact")]
        public async Task<IActionResult> SaveArtifact([FromBody] SaveArtifactRequest req)
        {
            await _agentService.SaveArtifactAsync(req);
            return Ok(new { Status = "Artifact saved as context successfully" });
        }
    }
}