using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaidenAIAgent.Shared.Services;

namespace MaidenAIAgent.Infrastructure.Services
{
    /// <summary>
    /// A decorator for ClaudeService that adds rate limiting capabilities
    /// </summary>
    public class RateLimitedClaudeService : ILLMService
    {
        private readonly ClaudeService _innerService;
        private readonly IRateLimiter _rateLimiter;
        private readonly ILogger<RateLimitedClaudeService> _logger;

        // Rate limiting resource keys
        private const string CLAUDE_REQUESTS_KEY = "claude_requests";
        private const string CLAUDE_TOKENS_KEY = "claude_tokens";

        /// <summary>
        /// Maximum number of retry attempts for rate-limited requests
        /// </summary>
        private const int MAX_RETRY_ATTEMPTS = 3;

        public RateLimitedClaudeService(
            ClaudeService innerService,
            IRateLimiter rateLimiter,
            ILogger<RateLimitedClaudeService> logger)
        {
            _innerService = innerService;
            _rateLimiter = rateLimiter;
            _logger = logger;
        }

        /// <summary>
        /// Generates a response from Claude with rate limiting
        /// </summary>
        public async Task<LLMResponse> GenerateResponseAsync(
            string query,
            string? context = null,
            string? systemPrompt = null)
        {
            // First check if we're allowed to make a request
            if (!await _rateLimiter.TryAcquireAsync(CLAUDE_REQUESTS_KEY))
            {
                var waitTime = await _rateLimiter.GetTimeUntilNextPermittedRequestAsync(CLAUDE_REQUESTS_KEY);

                _logger.LogWarning(
                    "Rate limit exceeded for Claude API requests. Retry after {WaitTimeMs}ms",
                    waitTime.TotalMilliseconds);

                // If we can't make the request, return an error response
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = $"Rate limit exceeded for Claude API. Please try again in {waitTime.TotalSeconds:N1} seconds.",
                    Metadata = new Dictionary<string, object>
                    {
                        { "rate_limited", true },
                        { "retry_after_ms", waitTime.TotalMilliseconds }
                    }
                };
            }

            // Next check if we have token capacity
            if (!await _rateLimiter.TryAcquireAsync(CLAUDE_TOKENS_KEY))
            {
                var waitTime = await _rateLimiter.GetTimeUntilNextPermittedRequestAsync(CLAUDE_TOKENS_KEY);

                _logger.LogWarning(
                    "Token rate limit exceeded for Claude API. Retry after {WaitTimeMs}ms",
                    waitTime.TotalMilliseconds);

                // If we don't have token capacity, return an error response
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = $"Token rate limit exceeded for Claude API. Please try again in {waitTime.TotalSeconds:N1} seconds.",
                    Metadata = new Dictionary<string, object>
                    {
                        { "rate_limited", true },
                        { "retry_after_ms", waitTime.TotalMilliseconds }
                    }
                };
            }

            try
            {
                // We're allowed to make the request, delegate to the inner service
                var response = await _innerService.GenerateResponseAsync(query, context, systemPrompt);

                if (response.Success && response.TokensUsed.HasValue)
                {
                    // Record the actual token usage
                    await _rateLimiter.RecordSuccessfulRequestAsync(
                        CLAUDE_TOKENS_KEY,
                        (int)response.TokensUsed.Value);
                }

                return response;
            }
            catch (Exception ex) when (IsRateLimitException(ex))
            {
                // Handle rate limiting errors from the API
                _logger.LogWarning(ex, "Claude API rate limit error: {Message}", ex.Message);

                // Retry after delay if possible
                // In a real implementation, you might want to parse the retry-after header
                TimeSpan retryDelay = TimeSpan.FromSeconds(2);

                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "Claude API rate limit reached. Please try again later.",
                    Metadata = new Dictionary<string, object>
                    {
                        { "rate_limited", true },
                        { "retry_after_ms", retryDelay.TotalMilliseconds }
                    }
                };
            }
            catch (Exception ex)
            {
                // For other errors, log and pass through
                _logger.LogError(ex, "Error calling Claude API: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Determines if the exception is related to rate limiting
        /// </summary>
        private bool IsRateLimitException(Exception ex)
        {
            // In a real implementation, you'd check for specific exception types
            // or status codes returned by the Claude API
            return ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("too many requests", StringComparison.OrdinalIgnoreCase);
        }
    }
}