namespace MaidenAIAgent.Core.Tools
{
    public class ToolResult
    {
        public bool Success { get; set; }
        public string Result { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
