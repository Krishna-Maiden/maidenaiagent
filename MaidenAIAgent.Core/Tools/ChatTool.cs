using MaidenAIAgent.Shared.Services;
using Microsoft.Extensions.Options;

namespace MaidenAIAgent.Core.Tools
{
    public class ChatTool : ITool
    {
        private readonly ILLMService _llmService;
        private readonly ChatToolSettings _settings;

        public string Name => "Chat";
        public string Description => "Engages in conversation using Claude 3.7 Sonnet for intelligent responses";

        // List of patterns that indicate a conversational query
        private readonly string[] _chatPatterns = new[]
        {
            "hello", "hi", "hey", "help", "thanks", "thank you",
            "how are you", "what can you do", "tell me", "chat",
            "talk", "converse", "assist", "guide", "explain"
        };

        public ChatTool(ILLMService llmService, IOptions<ChatToolSettings> settings)
        {
            _llmService = llmService;
            _settings = settings.Value;
        }

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
                            { "used_llm", false }
                        }
                    };
                }

                // For more complex queries, use Claude
                var systemPrompt = _settings.SystemPrompt;

                // Add context from parameters if available
                string? context = null;
                if (parameters.ContainsKey("context"))
                {
                    context = parameters["context"];
                }

                // Get response from Claude
                var llmResponse = await _llmService.GenerateResponseAsync(query, context, systemPrompt);

                if (!llmResponse.Success)
                {
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
                        { "tokens_used", llmResponse.TokensUsed ?? 0 },
                        { "model", llmResponse.Metadata.GetValueOrDefault("model", "unknown") }
                    }
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error in ChatTool: {ex.Message}"
                };
            }
        }

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

    public class ChatToolSettings
    {
        public string SystemPrompt { get; set; } = "You are a helpful AI assistant integrated into an AI Agent system. Provide concise, accurate, and helpful responses to the user's queries. Keep responses under 3 paragraphs unless specifically asked for more detail.";
        public int ResponseMaxLength { get; set; } = 500;
        public bool UseCache { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 60;
    }
}