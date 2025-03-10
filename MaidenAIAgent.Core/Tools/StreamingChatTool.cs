using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaidenAIAgent.Shared.Services;

namespace MaidenAIAgent.Core.Tools
{
    /// <summary>
    /// Chat tool that supports streaming responses for long-form content
    /// </summary>
    public class StreamingChatTool : ITool
    {
        private readonly IStreamingLLMService _llmService;
        private readonly ChatToolSettings _settings;
        private readonly ILogger<StreamingChatTool> _logger;

        public string Name => "StreamingChat";
        public string Description => "Engages in conversation with streaming support for long responses";

        // List of patterns that indicate a conversational query
        private readonly string[] _chatPatterns = new[]
        {
            "hello", "hi", "hey", "help", "thanks", "thank you",
            "how are you", "what can you do", "tell me", "chat",
            "talk", "converse", "assist", "guide", "explain"
        };

        public StreamingChatTool(
            IStreamingLLMService llmService,
            IOptions<ChatToolSettings> settings,
            ILogger<StreamingChatTool> logger)
        {
            _llmService = llmService;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Checks if this tool can handle the given query
        /// </summary>
        public bool CanHandle(string query)
        {
            // If any other tool should specifically handle this query, let them
            // This makes Chat a lower priority fallback tool
            if (IsSpecificToolQuery(query))
            {
                return false;
            }

            // Check if the query contains any chat patterns
            string lowerQuery = query.ToLower();

            // If query is very short, likely a greeting
            if (query.Length < 10)
            {
                return _chatPatterns.Any(pattern => lowerQuery.Contains(pattern));
            }

            // For longer queries, use Claude as a fallback for general queries
            return true;
        }

        /// <summary>
        /// Executes the chat query with support for streaming responses
        /// </summary>
        public async Task<ToolResult> ExecuteAsync(string query, Dictionary<string, string> parameters)
        {
            try
            {
                // If it's a simple query that can be handled with a predefined response
                if (IsSimpleQuery(query))
                {
                    string simpleResponse = GenerateSimpleResponse(query);
                    return new ToolResult
                    {
                        Success = true,
                        Result = simpleResponse,
                        Data = new Dictionary<string, object>
                        {
                            { "query", query },
                            { "responseType", "simple" },
                            { "used_llm", false },
                            { "streaming", false }
                        }
                    };
                }

                // For more complex queries that need LLM, check if streaming is requested
                bool useStreaming = parameters.TryGetValue("useStreaming", out var streamingValue) &&
                                   streamingValue.ToLower() == "true";

                // Check if the query appears to need a long response
                bool isLongFormQuery = IsLongFormQuery(query);

                // Use streaming for long-form queries or when explicitly requested
                if (useStreaming || isLongFormQuery)
                {
                    // For streaming responses, we return a channel reader in the Data dictionary
                    var systemPrompt = _settings.SystemPrompt;

                    // Add context from parameters if available
                    string? context = null;
                    if (parameters.TryGetValue("context", out var contextValue))
                    {
                        context = contextValue;
                    }

                    // Create cancellation token source for timeout
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.ResponseTimeoutSeconds));

                    // Start streaming response
                    var responseStream = await _llmService.GenerateStreamingResponseAsync(
                        query, context, systemPrompt, cts.Token);

                    // Create a proxy channel to pass to the caller
                    var channel = Channel.CreateUnbounded<StreamingResponseChunk>();

                    // Start a background task to forward chunks and handle completion
                    _ = Task.Run(async () =>
                    {
                        var fullResponse = new StringBuilder();

                        try
                        {
                            await foreach (var chunk in responseStream.ReadAllAsync(cts.Token))
                            {
                                // Add to the full response
                                if (!string.IsNullOrEmpty(chunk.Content))
                                {
                                    fullResponse.Append(chunk.Content);
                                }

                                // Forward chunk to our channel
                                await channel.Writer.WriteAsync(chunk, cts.Token);

                                // If this is the last chunk or there was an error, complete the channel
                                if (chunk.IsComplete || !string.IsNullOrEmpty(chunk.Error))
                                {
                                    channel.Writer.Complete();
                                    break;
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // If cancelled, send a final chunk indicating this
                            await channel.Writer.WriteAsync(new StreamingResponseChunk
                            {
                                Error = "Response streaming was cancelled or timed out",
                                IsComplete = true
                            });
                            channel.Writer.Complete();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing streaming response");

                            // Send error to channel
                            await channel.Writer.WriteAsync(new StreamingResponseChunk
                            {
                                Error = $"Error processing streaming response: {ex.Message}",
                                IsComplete = true
                            });
                            channel.Writer.Complete();
                        }
                    });

                    // Return immediately with the channel reader
                    return new ToolResult
                    {
                        Success = true,
                        Result = "Streaming response started",
                        Data = new Dictionary<string, object>
                        {
                            { "query", query },
                            { "responseType", "complex" },
                            { "used_llm", true },
                            { "streaming", true },
                            { "streamingChannel", responseStream }
                        }
                    };
                }
                else
                {
                    // For non-streaming responses, use the standard LLM service
                    var systemPrompt = _settings.SystemPrompt;

                    // Add context from parameters if available
                    string? context = null;
                    if (parameters.TryGetValue("context", out var contextValue))
                    {
                        context = contextValue;
                    }

                    // Get response from Claude (non-streaming)
                    var llmResponse = await _llmService.GenerateResponseAsync(query, context, systemPrompt);

                    if (!llmResponse.Success)
                    {
                        // Check if this was a rate limiting issue
                        if (llmResponse.Metadata.TryGetValue("rate_limited", out var rateLimited) &&
                            (bool)rateLimited == true)
                        {
                            // Return a friendly message about rate limiting
                            return new ToolResult
                            {
                                Success = true, // Still mark as success to avoid error UI
                                Result = "I'm currently handling a high volume of requests. " +
                                        (llmResponse.ErrorMessage ?? "Please try again in a moment."),
                                Data = new Dictionary<string, object>
                                {
                                    { "query", query },
                                    { "responseType", "rate_limited" },
                                    { "used_llm", false },
                                    { "streaming", false },
                                    { "retry_after_ms", llmResponse.Metadata.GetValueOrDefault("retry_after_ms", 5000) }
                                }
                            };
                        }

                        return new ToolResult
                        {
                            Success = false,
                            ErrorMessage = $"Failed to get response from Claude: {llmResponse.ErrorMessage}"
                        };
                    }

                    return new ToolResult
                    {
                        Success = true,
                        Result = llmResponse.Content,
                        Data = new Dictionary<string, object>
                        {
                            { "query", query },
                            { "responseType", "complex" },
                            { "used_llm", true },
                            { "streaming", false },
                            { "tokens_used", llmResponse.TokensUsed ?? 0 },
                            { "model", llmResponse.Metadata.GetValueOrDefault("model", "unknown") }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StreamingChatTool: {Message}", ex.Message);

                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error in StreamingChatTool: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Checks if a query is simple enough to handle with predefined responses
        /// </summary>
        private bool IsSimpleQuery(string query)
        {
            string lowerQuery = query.ToLower();

            // Check for simple greetings or common phrases
            return lowerQuery.Length < 10 &&
                   (_chatPatterns.Any(pattern => lowerQuery.Contains(pattern)) ||
                    lowerQuery == "hello" ||
                    lowerQuery == "hi" ||
                    lowerQuery == "hey" ||
                    lowerQuery == "thanks" ||
                    lowerQuery == "thank you");
        }

        /// <summary>
        /// Checks if a query is likely to need a long-form response
        /// </summary>
        private bool IsLongFormQuery(string query)
        {
            string lowerQuery = query.ToLower();

            // Check for keywords that suggest a lengthy response would be appropriate
            string[] longFormIndicators = new[]
            {
                "explain in detail", "comprehensive", "elaborate", "in depth",
                "tell me everything", "write a", "generate a", "create a",
                "detailed explanation", "extensive", "thorough", "analyze", "list all",
                "compare and contrast", "history of", "essay", "article", "tutorial",
                "step by step", "guide", "how do I", "examples of"
            };

            if (longFormIndicators.Any(indicator => lowerQuery.Contains(indicator)))
            {
                return true;
            }

            // Check query length - longer queries often need longer responses
            if (query.Length > 100)
            {
                return true;
            }

            // Check for question marks - multiple questions may need detailed answers
            if (query.Count(c => c == '?') > 1)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generates a simple response for basic queries
        /// </summary>
        private string GenerateSimpleResponse(string query)
        {
            string lowerQuery = query.ToLower();

            // Handle greetings
            if (lowerQuery.Contains("hello") || lowerQuery.Contains("hi") || lowerQuery.Contains("hey"))
            {
                return "Hello! How can I assist you today?";
            }

            // Handle help requests
            if (lowerQuery.Contains("help") || lowerQuery.Contains("what can you do"))
            {
                return "I can help you with several tasks. You can ask me to search for information, calculate mathematical expressions, check the weather, or just chat about any topic!";
            }

            // Handle thank you
            if (lowerQuery.Contains("thank") || lowerQuery.Contains("thanks"))
            {
                return "You're welcome! Is there anything else I can help you with?";
            }

            // Handle how are you
            if (lowerQuery.Contains("how are you"))
            {
                return "I'm functioning well, thank you for asking! How can I assist you today?";
            }

            // Default response
            return "I'm here to help. You can ask me to search for information, calculate expressions, check the weather, or just chat. What would you like to know?";
        }

        /// <summary>
        /// Checks if the query is specifically for another tool
        /// </summary>
        private bool IsSpecificToolQuery(string query)
        {
            string lowerQuery = query.ToLower();

            // Weather-specific patterns
            if (lowerQuery.Contains("weather") ||
                lowerQuery.Contains("temperature") ||
                lowerQuery.Contains("forecast"))
            {
                return true;
            }

            // Calculator-specific patterns
            if (lowerQuery.Contains("calculate") ||
                lowerQuery.Contains("compute") ||
                lowerQuery.Contains("+") ||
                lowerQuery.Contains("-") ||
                lowerQuery.Contains("*") ||
                lowerQuery.Contains("/"))
            {
                return true;
            }

            // Search-specific patterns
            if (lowerQuery.Contains("search for") ||
                lowerQuery.Contains("find info") ||
                lowerQuery.Contains("look up"))
            {
                return true;
            }

            return false;
        }
    }
}