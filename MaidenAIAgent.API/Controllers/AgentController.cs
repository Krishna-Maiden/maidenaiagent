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
        private readonly ILogger<AgentController> _logger;

        public AgentController(IAgentService agentService, ILogger<AgentController> logger)
        {
            _agentService = agentService;
            _logger = logger;
        }

        [HttpGet("test")]
        public ActionResult<string> Test()
        {
            return Ok("API is working correctly");
        }

        [HttpPost("process")]
        public async Task<ActionResult<AgentResponse>> ProcessQuery([FromBody] AgentRequest request)
        {
            if (string.IsNullOrEmpty(request.Query))
            {
                return BadRequest("Query cannot be empty");
            }

            try
            {
                var response = await _agentService.ProcessQueryAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query: {Query}", request.Query);
                return StatusCode(500, new { Error = $"Error processing query: {ex.Message}" });
            }
        }

        [HttpGet("tools")]
        public ActionResult<IEnumerable<ToolInfo>> GetAvailableTools()
        {
            try
            {
                // Get all registered tools from the service
                var tools = _agentService.GetAvailableTools();

                // Make sure we have tools
                if (!tools.Any())
                {
                    _logger.LogWarning("No tools were found in the registry");
                    return Problem("No tools were found in the registry", statusCode: 500);
                }

                // Log the tools we found
                _logger.LogInformation("Found {Count} tools: {Tools}",
                    tools.Count(),
                    string.Join(", ", tools.Select(t => t.Name)));

                return Ok(tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tools");
                return Problem($"Error retrieving tools: {ex.Message}", statusCode: 500);
            }
        }

        [HttpPost("testTool")]
        public async Task<ActionResult<ToolResult>> TestTool([FromBody] TestToolRequest request)
        {
            try
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
                var result = await toolOrchestrator.ExecuteToolAsync(
                    request.ToolName,
                    request.Query,
                    request.Parameters ?? new Dictionary<string, string>());

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing tool: {ToolName}, Query: {Query}",
                    request.ToolName, request.Query);
                return StatusCode(500, new { Error = $"Error testing tool: {ex.Message}" });
            }
        }
    }

    public class TestToolRequest
    {
        public string ToolName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public Dictionary<string, string>? Parameters { get; set; }
    }
}