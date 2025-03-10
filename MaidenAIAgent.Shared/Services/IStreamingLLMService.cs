using System.Threading.Channels;

namespace MaidenAIAgent.Shared.Services
{
    /// <summary>
    /// Interface for large language model services that support streaming responses
    /// </summary>
    public interface IStreamingLLMService : ILLMService
    {
        /// <summary>
        /// Sends a query to the large language model and returns a streaming response
        /// </summary>
        /// <param name="query">The user query</param>
        /// <param name="context">Optional additional context for the query</param>
        /// <param name="systemPrompt">Optional system prompt to guide the model's behavior</param>
        /// <param name="cancellationToken">Cancellation token to cancel the streaming operation</param>
        /// <returns>A channel of streaming content chunks</returns>
        Task<ChannelReader<StreamingResponseChunk>> GenerateStreamingResponseAsync(
            string query,
            string? context = null,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a single chunk of a streaming response
    /// </summary>
    public class StreamingResponseChunk
    {
        /// <summary>
        /// The text content of this chunk
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this is the final chunk in the response
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Any error that occurred during streaming
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Additional metadata about this chunk
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}