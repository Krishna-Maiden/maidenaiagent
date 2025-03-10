namespace MaidenAIAgent.Core.Tools
{
    public class ToolRegistry : IToolRegistry
    {
        private readonly IEnumerable<ITool> _tools;

        public ToolRegistry(IEnumerable<ITool> tools)
        {
            _tools = tools;
        }

        public ITool GetTool(string name)
        {
            return _tools.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<ITool> GetAllTools()
        {
            return _tools;
        }

        public ITool FindBestToolForQuery(string query)
        {
            return _tools.FirstOrDefault(t => t.CanHandle(query));
        }

        public Task<ITool> FindBestToolForQueryAsync(string query)
        {
            // For backward compatibility, the default implementation just calls the synchronous version
            return Task.FromResult(FindBestToolForQuery(query));
        }
    }
}