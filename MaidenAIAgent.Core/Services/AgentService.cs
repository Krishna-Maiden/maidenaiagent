using MaidenAIAgent.Core.Models;
using MaidenAIAgent.Core.Tools;

namespace MaidenAIAgent.Core.Services
{
    public class AgentService : IAgentService
    {
        private readonly IToolRegistry _toolRegistry;

        public AgentService(IToolRegistry toolRegistry)
        {
            _toolRegistry = toolRegistry;
        }

        public async Task<AgentResponse> ProcessQueryAsync(AgentRequest request)
        {
            try
            {
                ITool tool;

                if (request.UseAllTools)
                {
                    tool = _toolRegistry.FindBestToolForQuery(request.Query);
                }
                else if (request.SpecificTools.Any())
                {
                    tool = _toolRegistry.GetTool(request.SpecificTools.First());
                }
                else
                {
                    return new AgentResponse
                    {
                        Success = false,
                        ErrorMessage = "No suitable tool found for the query"
                    };
                }

                if (tool == null)
                {
                    return new AgentResponse
                    {
                        Success = false,
                        Response = "I don't know how to handle this query. Please try something else.",
                        ErrorMessage = "No suitable tool found"
                    };
                }

                var result = await tool.ExecuteAsync(request.Query, request.Parameters);

                return new AgentResponse
                {
                    Success = result.Success,
                    Response = result.Result,
                    ToolUsed = tool.Name,
                    Data = result.Data,
                    ErrorMessage = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                return new AgentResponse
                {
                    Success = false,
                    ErrorMessage = $"Error processing query: {ex.Message}"
                };
            }
        }

        public IEnumerable<ToolInfo> GetAvailableTools()
        {
            return _toolRegistry.GetAllTools().Select(t => new ToolInfo
            {
                Name = t.Name,
                Description = t.Description
            });
        }
    }
}
