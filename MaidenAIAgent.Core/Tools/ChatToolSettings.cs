namespace MaidenAIAgent.Core.Tools
{
    public class ChatToolSettings
    {
        public string SystemPrompt { get; set; } = "You are a helpful AI assistant integrated into an AI Agent system. Provide concise, accurate, and helpful responses to the user's queries. Keep responses under 3 paragraphs unless specifically asked for more detail.";
        public int ResponseMaxLength { get; set; } = 500;
        public bool UseCache { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 60;
        public int ResponseTimeoutSeconds { get; set; } = 60;
        public bool DefaultToStreaming { get; set; } = true;
    }
}