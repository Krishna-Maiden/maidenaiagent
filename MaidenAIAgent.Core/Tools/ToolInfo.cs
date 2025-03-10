namespace MaidenAIAgent.Core.Tools
{
    public class ToolInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new List<string>();
    }
}
