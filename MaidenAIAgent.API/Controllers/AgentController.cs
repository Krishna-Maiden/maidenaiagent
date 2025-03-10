using Microsoft.AspNetCore.Mvc;
using MaidenAIAgent.Core.Models;
using MaidenAIAgent.Core.Services;
using MaidenAIAgent.Core.Tools;

namespace MaidenAIAgent.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly IAgentService _agentService;

        public AgentController(IAgentService agentService)
        {
            _agentService = agentService;
        }

        [HttpPost("process")]
        public async Task<ActionResult<AgentResponse>> ProcessQuery([FromBody] AgentRequest request)
        {
            if (string.IsNullOrEmpty(request.Query))
            {
                return BadRequest("Query cannot be empty");
            }

            var response = await _agentService.ProcessQueryAsync(request);
            return Ok(response);
        }

        [HttpGet("tools")]
        public ActionResult<IEnumerable<ToolInfo>> GetAvailableTools()
        {
            var tools = _agentService.GetAvailableTools();
            return Ok(tools);
        }
    }
}
