using MaidenAIAgent.Core.Models;
using MaidenAIAgent.Core.Tools;

namespace MaidenAIAgent.Core.Services
{
    public interface IAgentService
    {
        Task<AgentResponse> ProcessQueryAsync(AgentRequest request);
        IEnumerable<ToolInfo> GetAvailableTools();
    }
}
