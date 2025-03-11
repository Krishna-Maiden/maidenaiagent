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
        [HttpGet("test")]
        public ActionResult<string> Test()
        {
            return Ok("API is working");
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

        // New endpoint to test a specific tool directly
        [HttpPost("testTool")]
        public async Task<ActionResult<ToolResult>> TestTool([FromBody] TestToolRequest request)
        {
            if (string.IsNullOrEmpty(request.ToolName))
            {
                return BadRequest("Tool name cannot be empty");
            }

            if (string.IsNullOrEmpty(request.Query))
            {
                return BadRequest("Query cannot be empty");
            }

            var toolOrchestrator = HttpContext.RequestServices.GetRequiredService<IToolOrchestratorService>();
            var result = await toolOrchestrator.ExecuteToolAsync(request.ToolName, request.Query, request.Parameters ?? new Dictionary<string, string>());

            return Ok(result);
        }
    }

    public class TestToolRequest
    {
        public string ToolName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public Dictionary<string, string>? Parameters { get; set; }
    }
}