namespace MaidenAIAgent.Core.Tools
{
    public interface IToolRegistry
    {
        ITool GetTool(string name);
        IEnumerable<ITool> GetAllTools();
        ITool FindBestToolForQuery(string query);
        Task<ITool> FindBestToolForQueryAsync(string query);
    }
}