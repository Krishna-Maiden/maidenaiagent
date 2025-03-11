using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MaidenAIAgent.Core.Tools;
using MaidenAIAgent.Shared.Services;
using System.Text.Json;

namespace MaidenAIAgent.Core.Services
{
    /// <summary>
    /// Service that allows tools (especially Chat Tool) to use other tools as needed
    /// </summary>
    public interface IToolOrchestratorService
    {
        /// <summary>
        /// Executes a specific tool by name with the given query and parameters
        /// </summary>
        Task<ToolResult> ExecuteToolAsync(string toolName, string query, Dictionary<string, string> parameters);

        /// <summary>
        /// Finds the best tool for a query and executes it
        /// </summary>
        Task<ToolResult> ExecuteBestToolAsync(string query, Dictionary<string, string> parameters);

        /// <summary>
        /// Gets information about all available tools
        /// </summary>
        IEnumerable<ToolInfo> GetAllTools();
    }

    public class ToolOrchestratorService : IToolOrchestratorService
    {
        private readonly IToolRegistry _toolRegistry;
        private readonly ILogger<ToolOrchestratorService> _logger;

        public ToolOrchestratorService(
            IToolRegistry toolRegistry,
            ILogger<ToolOrchestratorService>? logger = null)
        {
            _toolRegistry = toolRegistry;
            _logger = logger ?? NullLogger<ToolOrchestratorService>.Instance;
        }

        /// <summary>
        /// Executes a specific tool by name with the given query and parameters
        /// </summary>
        public async Task<ToolResult> ExecuteToolAsync(string toolName, string query, Dictionary<string, string> parameters)
        {
            try
            {
                var tool = _toolRegistry.GetTool(toolName);

                if (tool == null)
                {
                    _logger.LogWarning("Tool not found: {ToolName}", toolName);
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"Tool not found: {toolName}",
                        Data = new Dictionary<string, object>
                        {
                            { "availableTools", _toolRegistry.GetAllTools().Select(t => t.Name).ToList() }
                        }
                    };
                }

                _logger.LogInformation("Executing tool {ToolName} with query: {Query}", toolName, query);
                return await tool.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}: {Message}", toolName, ex.Message);
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error executing tool {toolName}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Finds the best tool for a query and executes it
        /// </summary>
        public async Task<ToolResult> ExecuteBestToolAsync(string query, Dictionary<string, string> parameters)
        {
            try
            {
                var tool = await _toolRegistry.FindBestToolForQueryAsync(query);

                if (tool == null)
                {
                    _logger.LogWarning("No suitable tool found for query: {Query}", query);
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "No suitable tool found for this query"
                    };
                }

                // Don't allow recursive calls to chat tools to prevent infinite loops
                if (tool.Name == "Chat" || tool.Name == "StreamingChat" || tool.Name == "AugmentedChat")
                {
                    _logger.LogWarning("Prevented recursive call to Chat tool from orchestrator");
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Tool orchestration cannot call Chat tools to prevent recursive loops"
                    };
                }

                _logger.LogInformation("Using best tool {ToolName} for query: {Query}", tool.Name, query);
                return await tool.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding and executing best tool: {Message}", ex.Message);
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error executing best tool: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets information about all available tools
        /// </summary>
        public IEnumerable<ToolInfo> GetAllTools()
        {
            try
            {
                // Filter out Chat tools to prevent recursive calls
                return _toolRegistry.GetAllTools()
                    .Where(t => t.Name != "Chat" && t.Name != "StreamingChat" && t.Name != "AugmentedChat")
                    .Select(t => new ToolInfo
                    {
                        Name = t.Name,
                        Description = t.Description
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tools: {Message}", ex.Message);
                return Enumerable.Empty<ToolInfo>();
            }
        }
    }
}