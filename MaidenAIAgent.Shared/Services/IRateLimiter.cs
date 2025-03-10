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
}