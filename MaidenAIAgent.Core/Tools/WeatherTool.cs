namespace MaidenAIAgent.Core.Tools
{
    public class WeatherTool : ITool
    {
        public string Name => "Weather";
        public string Description => "Gets weather information for a location";

        public bool CanHandle(string query)
        {
            var weatherTerms = new[] { "weather", "temperature", "forecast", "sunny", "rainy", "cloudy" };
            return weatherTerms.Any(term => query.ToLower().Contains(term));
        }

        public async Task<ToolResult> ExecuteAsync(string query, Dictionary<string, string> parameters)
        {
            try
            {
                // Extract location from query
                string location = ExtractLocation(query);

                if (string.IsNullOrEmpty(location) && parameters.ContainsKey("location"))
                {
                    location = parameters["location"];
                }

                if (string.IsNullOrEmpty(location))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "No location specified. Please provide a location."
                    };
                }

                // In a real implementation, this would call a weather API
                await Task.Delay(500); // Simulate network call

                return new ToolResult
                {
                    Success = true,
                    Result = $"The weather in {location} is currently sunny and 72°F",
                    Data = new Dictionary<string, object>
                    {
                        { "location", location },
                        { "temperature", 72 },
                        { "condition", "sunny" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to get weather data: {ex.Message}"
                };
            }
        }

        private string ExtractLocation(string query)
        {
            // This is a simplified example
            // In a real implementation, this would use NLP or a parsing library
            var locationPatterns = new[] { "in ", "for ", "at " };

            foreach (var pattern in locationPatterns)
            {
                if (query.ToLower().Contains(pattern))
                {
                    var index = query.ToLower().IndexOf(pattern);
                    return query.Substring(index + pattern.Length).Trim().Split(' ')[0];
                }
            }

            return string.Empty;
        }
    }
}
