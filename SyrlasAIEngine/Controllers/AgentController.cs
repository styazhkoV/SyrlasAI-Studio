using Microsoft.AspNetCore.Mvc;
using SyrlasAIEngine.Models;
using SyrlasAIEngine.Services;
using System.Threading.Tasks;

namespace SyrlasAIEngine.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly AgentService _agentService;
        private readonly PromptFactory _promptFactory;

        public AgentController(AgentService agentService, PromptFactory promptFactory)
        {
            _agentService = agentService;
            _promptFactory = promptFactory;
        }

        [HttpGet("roles")]
        public IActionResult GetRoles()
        {
            return Ok(_promptFactory.GetAllProfiles());
        }

        [HttpPost("switch")]
        public async Task<IActionResult> SwitchRole([FromBody] SwitchRoleRequest req)
        {
            await _agentService.SwitchRoleAsync(req.SessionId, req.Role);
            return Ok(new { Status = "Role switched successfully", ActiveRole = req.Role });
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest req)
        {
            var response = await _agentService.ProcessChatAsync(req);
            return Ok(response);
        }
    }
}