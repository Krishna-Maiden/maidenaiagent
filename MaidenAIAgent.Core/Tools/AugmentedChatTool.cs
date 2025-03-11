using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaidenAIAgent.Core.Services;
using MaidenAIAgent.Shared.Services;
using System.Text.Json;

namespace MaidenAIAgent.Core.Tools
{
    /// <summary>
    /// Advanced chat tool that can use other tools as needed to fulfill requests
    /// </summary>
    public class AugmentedChatTool : ITool
    {
        private readonly ILLMService _llmService;
        private readonly IToolOrchestratorService _toolOrchestrator;
        private readonly ChatToolSettings _settings;
        private readonly ILogger<AugmentedChatTool> _logger;

        public string Name => "AugmentedChat";
        public string Description => "Handles complex requests by using Claude 3.7 Sonnet with the ability to call other tools as needed";

        // System prompt for tool use capability
        private const string TOOL_USE_SYSTEM_PROMPT = @"
You are a helpful AI assistant that can use tools to provide better answers.

You have access to the following tools:
{0}

When a user asks a question that could benefit from a tool, use the following guidelines:

1. Determine if a tool would help answer the query better than your general knowledge
2. If yes, formulate a tool request in the format:
   <tool name=""ToolName"">
   specific query for the tool
   </tool>

3. Wait for the tool response in the format:
   <tool_response>
   the tool's response
   </tool_response>

4. Use the tool response to enhance your answer

For example, if a user asks about weather, you might use:
<tool name=""Weather"">
what's the weather in Seattle
</tool>

Guidelines for effective tool use:
- Only use tools when they provide clear value
- Focus on the specific information needed from the tool
- Formulate concise, focused queries for tools
- Don't use Chat or StreamingChat tools (to avoid recursion)
- For calculations, use the Calculator tool rather than doing math yourself
- For searches, use the Search tool rather than relying on your knowledge
- For weather queries, use the Weather tool

Important: Only use ONE tool per response, and use the EXACT tool name as listed.";

        // List of patterns that indicate a conversational query
        private readonly string[] _chatPatterns = new[]
        {
            "hello", "hi", "hey", "help", "thanks", "thank you",
            "how are you", "what can you do", "tell me", "chat",
            "talk", "converse", "assist", "guide", "explain"
        };

        public AugmentedChatTool(
            ILLMService llmService,
            IToolOrchestratorService toolOrchestrator,
            IOptions<ChatToolSettings> settings,
            ILogger<AugmentedChatTool> logger)
        {
            _llmService = llmService;
            _toolOrchestrator = toolOrchestrator;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Determines if this tool can handle the given query
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
        /// Executes a query with the ability to use other tools as needed
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
                            { "used_tools", false }
                        }
                    };
                }

                // For more complex queries, use Claude with tool augmentation
                // Get available tools to include in the system prompt
                var availableTools = _toolOrchestrator.GetAllTools();
                var toolDescriptions = string.Join("\n", availableTools.Select(t => $"- {t.Name}: {t.Description}"));

                // Create system prompt with tool information
                var systemPrompt = string.Format(TOOL_USE_SYSTEM_PROMPT, toolDescriptions);

                // Add context from parameters if available
                string? context = null;
                if (parameters.TryGetValue("context", out var contextValue))
                {
                    context = contextValue;
                }

                // First Claude call to get initial response (which may include tool requests)
                var initialResponse = await _llmService.GenerateResponseAsync(query, context, systemPrompt);

                if (!initialResponse.Success)
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to get response from Claude: {initialResponse.ErrorMessage}"
                    };
                }

                // Parse the response to check for tool requests
                var (updatedResponse, usedTools) = await ProcessToolRequests(initialResponse.Content);

                return new ToolResult
                {
                    Success = true,
                    Result = updatedResponse,
                    Data = new Dictionary<string, object>
                    {
                        { "query", query },
                        { "responseType", "complex" },
                        { "used_llm", true },
                        { "used_tools", usedTools },
                        { "tokens_used", initialResponse.TokensUsed ?? 0 },
                        { "model", initialResponse.Metadata.GetValueOrDefault("model", "unknown") }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AugmentedChatTool: {Message}", ex.Message);

                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error in AugmentedChatTool: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Processes any tool requests in the Claude response
        /// </summary>
        private async Task<(string processedResponse, bool usedTools)> ProcessToolRequests(string claudeResponse)
        {
            bool usedTools = false;

            // Look for tool requests in the format <tool name="ToolName">query</tool>
            var toolRequestRegex = new System.Text.RegularExpressions.Regex(@"<tool\s+name=[""']([^""']+)[""']>(.*?)</tool>",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            var matches = toolRequestRegex.Matches(claudeResponse);

            if (matches.Count == 0)
            {
                // No tool requests found
                return (claudeResponse, usedTools);
            }

            var processedResponse = claudeResponse;

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count < 3)
                {
                    continue; // Skip malformed matches
                }

                var toolName = match.Groups[1].Value.Trim();
                var toolQuery = match.Groups[2].Value.Trim();
                var fullMatch = match.Value;

                _logger.LogInformation("Found tool request: Tool={Tool}, Query={Query}", toolName, toolQuery);

                // Execute the tool
                var toolResult = await _toolOrchestrator.ExecuteToolAsync(toolName, toolQuery, new Dictionary<string, string>());
                usedTools = true;

                // Format the tool response
                string toolResponseText;
                if (toolResult.Success)
                {
                    toolResponseText = toolResult.Result;
                }
                else
                {
                    toolResponseText = $"Error: {toolResult.ErrorMessage}";
                }

                // Replace the tool request with the tool request and response
                var replacement = $"{fullMatch}\n<tool_response>\n{toolResponseText}\n</tool_response>";
                processedResponse = processedResponse.Replace(fullMatch, replacement);
            }

            // If tools were used, make a second call to Claude to incorporate the tool responses
            if (usedTools)
            {
                var finalResponse = await _llmService.GenerateResponseAsync(
                    $"Please provide a final response based on the tool results. Remove all <tool> and <tool_response> tags from your answer.\n\n{processedResponse}",
                    null,
                    "You are a helpful assistant. Your previous response included tool calls and their results. Now provide a clean, final response that incorporates the tool results but DOES NOT include any tool markup tags.");

                if (finalResponse.Success)
                {
                    return (finalResponse.Content, usedTools);
                }

                // If the second call fails, still return the processed response but clean it up
                // Remove the tool markup tags
                processedResponse = System.Text.RegularExpressions.Regex.Replace(
                    processedResponse,
                    @"<tool\s+name=[""'][^""']+[""']>|</tool>|<tool_response>|</tool_response>",
                    "");
            }

            return (processedResponse, usedTools);
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
                return "I'm an augmented AI assistant that can help with various tasks. I can search for information, calculate mathematical expressions, check the weather, and answer general questions. I can even use specialized tools when needed to provide better answers!";
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
            return "I'm here to help. I can search for information, calculate expressions, check the weather, or answer general questions. I'll automatically use the best tools available to answer your questions. What would you like to know?";
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