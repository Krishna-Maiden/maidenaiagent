namespace MaidenAIAgent.Shared.Services
{
    public interface ILLMService
    {
        /// <summary>
        /// Sends a query to the large language model and returns the response
        /// </summary>
        /// <param name="query">The user query</param>
        /// <param name="context">Optional additional context for the query</param>
        /// <param name="systemPrompt">Optional system prompt to guide the model's behavior</param>
        /// <returns>The response from the language model</returns>
        Task<LLMResponse> GenerateResponseAsync(string query, string? context = null, string? systemPrompt = null);
    }

    public class LLMResponse
    {
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double? TokensUsed { get; set; }
    }
}