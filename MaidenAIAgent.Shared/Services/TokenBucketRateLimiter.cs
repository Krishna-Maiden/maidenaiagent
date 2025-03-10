using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MaidenAIAgent.Shared.Services;

namespace MaidenAIAgent.Infrastructure.Services
{
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

    /// <summary>
    /// Implements a token bucket algorithm for rate limiting API calls
    /// </summary>
    public class TokenBucketRateLimiter : IRateLimiter
    {
        private readonly IMemoryCache _cache;
        private readonly RateLimiterSettings _settings;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // Resource keys for different rate limiting buckets
        private const string CLAUDE_REQUESTS_KEY = "claude_requests";
        private const string CLAUDE_TOKENS_KEY = "claude_tokens";

        public TokenBucketRateLimiter(IMemoryCache cache, IOptions<RateLimiterSettings> settings)
        {
            _cache = cache;
            _settings = settings.Value;
        }

        /// <summary>
        /// Attempts to acquire permission to make an API call
        /// </summary>
        public async Task<bool> TryAcquireAsync(string resourceKey)
        {
            await _semaphore.WaitAsync();

            try
            {
                var bucket = await GetOrCreateBucketAsync(resourceKey);

                // Check if there are tokens available in the bucket
                if (bucket.AvailableTokens >= 1)
                {
                    // Tokens are available, consume one
                    bucket.AvailableTokens -= 1;
                    bucket.LastRefillTimestamp = DateTime.UtcNow;
                    UpdateBucket(resourceKey, bucket);
                    return true;
                }

                // No tokens available, request should be throttled
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the time until the next permitted request
        /// </summary>
        public async Task<TimeSpan> GetTimeUntilNextPermittedRequestAsync(string resourceKey)
        {
            await _semaphore.WaitAsync();

            try
            {
                var bucket = await GetOrCreateBucketAsync(resourceKey);

                if (bucket.AvailableTokens >= 1)
                {
                    return TimeSpan.Zero;
                }

                // Calculate time until next token is available
                var timeSinceLastRefill = DateTime.UtcNow - bucket.LastRefillTimestamp;
                var refillRate = GetRefillRatePerMillisecond(resourceKey);
                var tokensAccrued = timeSinceLastRefill.TotalMilliseconds * refillRate;

                if (tokensAccrued < 1)
                {
                    var millisecondsUntilNextToken = (1 - tokensAccrued) / refillRate;
                    return TimeSpan.FromMilliseconds(millisecondsUntilNextToken);
                }

                return TimeSpan.Zero;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Records a successful API call for tracking purposes
        /// </summary>
        public async Task RecordSuccessfulRequestAsync(string resourceKey, int tokensUsed = 1)
        {
            await _semaphore.WaitAsync();

            try
            {
                // Only record token usage for the tokens bucket
                if (resourceKey == CLAUDE_TOKENS_KEY && tokensUsed > 1)
                {
                    var bucket = await GetOrCreateBucketAsync(resourceKey);

                    // Deduct additional tokens beyond the one already consumed in TryAcquire
                    bucket.AvailableTokens -= (tokensUsed - 1);

                    // Ensure we don't go negative
                    if (bucket.AvailableTokens < 0)
                    {
                        bucket.AvailableTokens = 0;
                    }

                    UpdateBucket(resourceKey, bucket);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        #region Helper Methods

        private async Task<TokenBucket> GetOrCreateBucketAsync(string resourceKey)
        {
            if (!_cache.TryGetValue(resourceKey, out TokenBucket bucket))
            {
                // Create a new bucket with full capacity
                bucket = new TokenBucket
                {
                    AvailableTokens = GetMaxTokens(resourceKey),
                    LastRefillTimestamp = DateTime.UtcNow
                };

                // Store in cache
                UpdateBucket(resourceKey, bucket);
            }
            else
            {
                // Refill tokens based on time elapsed since last refill
                await RefillBucketAsync(resourceKey, bucket);
            }

            return bucket;
        }

        private async Task RefillBucketAsync(string resourceKey, TokenBucket bucket)
        {
            var now = DateTime.UtcNow;
            var timeElapsed = now - bucket.LastRefillTimestamp;

            if (timeElapsed.TotalMilliseconds > 0)
            {
                var refillRate = GetRefillRatePerMillisecond(resourceKey);
                var tokensToAdd = timeElapsed.TotalMilliseconds * refillRate;

                if (tokensToAdd >= 1)
                {
                    bucket.AvailableTokens += tokensToAdd;

                    // Cap at maximum capacity
                    var maxTokens = GetMaxTokens(resourceKey);
                    if (bucket.AvailableTokens > maxTokens)
                    {
                        bucket.AvailableTokens = maxTokens;
                    }

                    // Update the last refill timestamp
                    bucket.LastRefillTimestamp = now;
                    UpdateBucket(resourceKey, bucket);
                }
            }
        }

        private void UpdateBucket(string resourceKey, TokenBucket bucket)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.CacheExpirationMinutes)
            };

            _cache.Set(resourceKey, bucket, cacheOptions);
        }

        private double GetRefillRatePerMillisecond(string resourceKey)
        {
            // Apply buffer to stay under the limit
            double bufferMultiplier = 1.0 - (_settings.BufferPercentage / 100.0);

            switch (resourceKey)
            {
                case CLAUDE_REQUESTS_KEY:
                    // Convert requests per minute to tokens per millisecond
                    return (_settings.ClaudeRequestsPerMinute * bufferMultiplier) / (60 * 1000);

                case CLAUDE_TOKENS_KEY:
                    // Convert tokens per minute to tokens per millisecond
                    return (_settings.ClaudeTokensPerMinute * bufferMultiplier) / (60 * 1000);

                default:
                    // Default fallback
                    return 10.0 / (60 * 1000);
            }
        }

        private double GetMaxTokens(string resourceKey)
        {
            switch (resourceKey)
            {
                case CLAUDE_REQUESTS_KEY:
                    // Maximum burst capacity is 10% of the per-minute limit
                    return Math.Max(1, _settings.ClaudeRequestsPerMinute * 0.1);

                case CLAUDE_TOKENS_KEY:
                    // Maximum burst capacity is 10% of the per-minute limit
                    return Math.Max(1000, _settings.ClaudeTokensPerMinute * 0.1);

                default:
                    return 5;
            }
        }

        #endregion

        /// <summary>
        /// Represents a token bucket for rate limiting
        /// </summary>
        private class TokenBucket
        {
            /// <summary>
            /// Number of tokens currently available in the bucket
            /// </summary>
            public double AvailableTokens { get; set; }

            /// <summary>
            /// Timestamp of the last bucket refill
            /// </summary>
            public DateTime LastRefillTimestamp { get; set; }
        }
    }
}