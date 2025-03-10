# Rate Limiting Documentation

## Overview

The MaidenAI Agent includes a robust rate limiting system to prevent excessive usage of external APIs, particularly for the Claude language model integration. This system helps to:

- Control costs by preventing unexpected API usage spikes
- Stay within API provider rate limits to avoid service disruptions
- Ensure fair resource allocation for all users of the system
- Gracefully handle throttling situations with informative responses

## Architecture

The rate limiting implementation uses the Token Bucket algorithm, a common approach for API rate limiting that allows for:

- Steady average request rates with the ability to handle occasional bursts
- Separate limits for different resources (e.g., request count vs. token count)
- Automatic token replenishment over time to ensure service availability

### Components

1. **IRateLimiter Interface**: Defines the contract for rate limiting services
2. **TokenBucketRateLimiter**: Implements the token bucket algorithm
3. **RateLimitedClaudeService**: Decorates the Claude service with rate limiting
4. **Configuration Settings**: Allows for flexible rate limit configuration

## Token Bucket Algorithm

The token bucket algorithm works by:

1. Maintaining a "bucket" of tokens that represents available capacity
2. Consuming tokens when API calls are made
3. Automatically refilling tokens at a steady rate over time
4. Rejecting requests when the bucket is empty (no tokens available)

This allows for:
- Handling request bursts (up to the bucket capacity)
- Maintaining a consistent average rate 
- Graceful recovery from high-demand periods

## Configuration

Rate limiting can be configured in the `appsettings.json` file under the `RateLimiterSettings` section:

```json
"RateLimiterSettings": {
  "ClaudeRequestsPerMinute": 10,
  "ClaudeTokensPerMinute": 10000,
  "BufferPercentage": 10,
  "CacheExpirationMinutes": 60
}
```

### Settings Explained

- **ClaudeRequestsPerMinute**: Maximum number of Claude API calls allowed per minute
- **ClaudeTokensPerMinute**: Maximum number of tokens (input + output) allowed per minute
- **BufferPercentage**: Safety buffer to stay under limits (0-100)
- **CacheExpirationMinutes**: How long to keep rate limiting data in memory

## Rate Limiting Behavior

When rate limits are reached, the system:

1. Returns a user-friendly error message indicating the service is temporarily busy
2. Provides an estimated wait time before the request can be retried
3. Logs detailed information about the rate limiting event
4. Continues to accept simple queries that don't require the LLM

## Example Scenarios

### Within Rate Limits

1. User sends a complex query
2. Rate limiter checks and approves the request
3. Claude API is called and returns a response
4. Token usage is recorded for future rate limiting calculations

### Exceeding Rate Limits

1. User sends a complex query during high system load
2. Rate limiter determines limits have been reached
3. Instead of calling Claude API, a friendly message is returned:
   - "I'm currently handling a high volume of requests. Please try again in a moment."
4. The UI can use the retry information to automatically retry after the suggested delay

## Implementation Details

### Request Rate Limiting

- Limits the number of API calls per minute
- Each API call consumes one token from the request bucket
- Tokens replenish at a rate of `ClaudeRequestsPerMinute / 60,000` per millisecond

### Token Rate Limiting

- Limits the total number of tokens processed per minute
- Initial API call consumes one token from the token bucket
- After the API call, the actual token usage is recorded
- Tokens replenish at a rate of `ClaudeTokensPerMinute / 60,000` per millisecond

### Rate Limit Enforcement

The `RateLimitedClaudeService` enforces rate limits by:

1. Checking request capacity before making an API call
2. Checking token capacity before making an API call
3. Recording actual token usage after a successful API call
4. Handling rate limit errors from the external API

## Best Practices for Working with Rate Limits

1. Design your application to gracefully handle rate limit responses
2. Implement exponential backoff for retrying rate-limited requests
3. Monitor rate limit usage through logs to identify patterns
4. Adjust rate limits based on actual usage patterns and requirements
5. Consider implementing user-based quotas for multi-tenant applications

## Monitoring and Troubleshooting

The rate limiter logs events to help with monitoring and troubleshooting:

- Warning logs when rate limits are reache