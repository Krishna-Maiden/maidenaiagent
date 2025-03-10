namespace MaidenAIAgent.Core.Tools
{
    public class CalculatorTool : ITool
    {
        public string Name => "Calculator";
        public string Description => "Performs mathematical calculations";

        public bool CanHandle(string query)
        {
            // Basic check for math-related queries
            var mathTerms = new[] { "calculate", "compute", "sum", "add", "subtract", "multiply", "divide" };

            // Check for math symbols
            var hasMathSymbols = query.Contains("+") || query.Contains("-") ||
                                query.Contains("*") || query.Contains("/") ||
                                query.Contains("=");

            return mathTerms.Any(term => query.ToLower().Contains(term)) || hasMathSymbols;
        }

        public async Task<ToolResult> ExecuteAsync(string query, Dictionary<string, string> parameters)
        {
            try
            {
                // Extract mathematical expression
                // This is a simplified example
                var expression = ExtractMathExpression(query);

                // In a real implementation, this would parse and evaluate the expression
                var result = "42"; // Placeholder

                return new ToolResult
                {
                    Success = true,
                    Result = $"The result is: {result}",
                    Data = new Dictionary<string, object>
                    {
                        { "expression", expression },
                        { "result", result }
                    }
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to calculate: {ex.Message}"
                };
            }
        }

        private string ExtractMathExpression(string query)
        {
            // This is a simplified example
            // In a real implementation, this would use regex or a parsing library
            return query.Contains("calculate") ?
                   query.Replace("calculate", "").Trim() : query;
        }
    }
}
