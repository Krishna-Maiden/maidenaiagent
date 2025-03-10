namespace MaidenAIAgent.Shared.Services
{
    /// <summary>
    /// Interface for rate limiting service to control API usage
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Attempts to acquire permission to make an API call
        /// </summary>
        /// <param name="resourceKey">Identifier for the resource being rate limited</param>
        /// <returns>True if the call is allowed, false if it should be throttled</returns>
        Task<bool> TryAcquireAsync(string resourceKey);

        /// <summary>
        /// Gets the time until the next permitted request
        /// </summary>
        /// <param name="resourceKey">Identifier for the resource being rate limited</param>
        /// <returns>Timespan until next permitted request, or TimeSpan.Zero if requests are currently allowed</returns>
        Task<TimeSpan> GetTimeUntilNextPermittedRequestAsync(string resourceKey);

        /// <summary>
        /// Records a successful API call for tracking purposes
        /// </summary>
        /// <param name="resourceKey">Identifier for the resource being rate limited</param>
        /// <param name="tokensUsed">Number of tokens consumed in the request (for token-based rate limiting)</param>
        Task RecordSuccessfulRequestAsync(string resourceKey, int tokensUsed = 1);
    }

    /// <summary>
    /// Configuration settings for the token bucket rate limiter
    /// </summary>
    public class RateLimiterSettings
    {
        /// <summary>
        /// Maximum requests per minute for Claude API
        /// </summary>
        public int ClaudeRequestsPerMinute { get; set; } = 10;

        /// <summary>
        /// Maximum tokens per minute for Claude API (both input and output)
        /// </summary>
        public int ClaudeTokensPerMinute { get; set; } = 10000;

        /// <summary>
        /// Buffer percentage to stay under the rate limit (0-100)
        /// </summary>
        public int BufferPercentage { get; set; } = 10;

        /// <summary>
        /// Cache expiration time for rate limiting data in minutes
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 60;
    }
}