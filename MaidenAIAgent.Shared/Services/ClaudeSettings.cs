namespace MaidenAIAgent.Shared.Services
{
    /// <summary>
    /// Configuration settings for Claude LLM service
    /// </summary>
    public class ClaudeSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public string ModelName { get; set; } = "claude-3-7-sonnet-20250219";
        public int MaxTokens { get; set; } = 1024;
        public double Temperature { get; set; } = 0.7;
        public string DefaultSystemPrompt { get; set; } = "You are Claude, an AI assistant created by Anthropic. You are helpful, harmless, and honest.";
    }
}