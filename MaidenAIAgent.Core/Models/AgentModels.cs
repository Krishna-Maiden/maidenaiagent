namespace MaidenAIAgent.Core.Models
{
    public class AgentRequest
    {
        public string Query { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public bool UseAllTools { get; set; } = true;
        public List<string> SpecificTools { get; set; } = new List<string>();
    }

    public class AgentResponse
    {
        public string Response { get; set; } = string.Empty;
        public string ToolUsed { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AgentSettings
    {
        public bool EnableAllTools { get; set; } = true;
        public List<string> DefaultEnabledTools { get; set; } = new List<string>();
        public int MaxConcurrentTools { get; set; } = 3;
        public int DefaultTimeoutSeconds { get; set; } = 30;
    }
}
