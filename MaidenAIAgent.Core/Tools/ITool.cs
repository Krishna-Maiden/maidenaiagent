namespace MaidenAIAgent.Core.Tools
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        bool CanHandle(string query);
        Task<ToolResult> ExecuteAsync(string query, Dictionary<string, string> parameters);
    }
}
