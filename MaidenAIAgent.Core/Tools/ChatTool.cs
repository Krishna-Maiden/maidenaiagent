namespace MaidenAIAgent.Core.Tools
{
    public class ChatTool : ITool
    {
        public string Name => "Chat";
        public string Description => "Engages in conversation and provides general assistance";

        // List of patterns that indicate a conversational query
        private readonly string[] _chatPatterns = new[]
        {
            "hello", "hi", "hey", "help", "thanks", "thank you",
            "how are you", "what can you do", "tell me", "chat",
            "talk", "converse", "assist", "guide", "explain"
        };

        public bool CanHandle(string query)
        {
            // Check if the query contains any chat patterns
            string lowerQuery = query.ToLower();

            // If query is very short, likely a greeting
            if (query.Length < 10)
            {
                return _chatPatterns.Any(pattern => lowerQuery.Contains(pattern));
            }

            // For longer queries, check if it starts with a chat pattern
            return _chatPatterns.Any(pattern => lowerQuery.StartsWith(pattern));
        }

        public async Task<ToolResult> ExecuteAsync(string query, Dictionary<string, string> parameters)
        {
            // Process the query to determine the appropriate response
            string response = GenerateResponse(query, parameters);

            // Simulate processing time
            await Task.Delay(300);

            return new ToolResult
            {
                Success = true,
                Result = response,
                Data = new Dictionary<string, object>
                {
                    { "query", query },
                    { "responseType", DetermineResponseType(query) }
                }
            };
        }

        private string GenerateResponse(string query, Dictionary<string, string> parameters)
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
                return "I can help you with several tasks. You can ask me to search for information, calculate mathematical expressions, check the weather, or just chat!";
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

        private string DetermineResponseType(string query)
        {
            string lowerQuery = query.ToLower();

            if (lowerQuery.Contains("hello") || lowerQuery.Contains("hi") || lowerQuery.Contains("hey"))
                return "greeting";

            if (lowerQuery.Contains("help") || lowerQuery.Contains("what can you do"))
                return "help";

            if (lowerQuery.Contains("thank"))
                return "gratitude";

            if (lowerQuery.Contains("how are you"))
                return "inquiry";

            return "general";
        }
    }
}