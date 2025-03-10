using Microsoft.Extensions.Logging;
using MaidenAIAgent.Shared.Services;

namespace MaidenAIAgent.Core.Tools
{
    /// <summary>
    /// Enhanced tool registry that uses NLP for more accurate tool selection
    /// </summary>
    public class EnhancedToolRegistry : IToolRegistry
    {
        private readonly IEnumerable<ITool> _tools;
        private readonly INLPService? _nlpService;
        private readonly ILogger<EnhancedToolRegistry> _logger;

        // Threshold for intent confidence to select a tool
        private const double CONFIDENCE_THRESHOLD = 0.65;

        public EnhancedToolRegistry(
            IEnumerable<ITool> tools,
            ILogger<EnhancedToolRegistry> logger,
            INLPService? nlpService = null)
        {
            _tools = tools;
            _nlpService = nlpService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a tool by name
        /// </summary>
        public ITool GetTool(string name)
        {
            return _tools.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all available tools
        /// </summary>
        public IEnumerable<ITool> GetAllTools()
        {
            return _tools;
        }

        /// <summary>
        /// Finds the best tool for a query using NLP if available
        /// </summary>
        public async Task<ITool> FindBestToolForQueryAsync(string query)
        {
            // If NLP service is available, use it for intent classification
            if (_nlpService != null)
            {
                try
                {
                    var intentResult = await _nlpService.ClassifyIntentAsync(query);

                    // Log the intent classification result
                    _logger.LogInformation(
                        "Query: '{Query}' classified as intent '{Intent}' with confidence {Confidence}",
                        query, intentResult.PrimaryIntent, intentResult.Confidence);

                    // If confidence is high enough and a tool is recommended
                    if (intentResult.Confidence >= CONFIDENCE_THRESHOLD &&
                        !string.IsNullOrEmpty(intentResult.RecommendedTool))
                    {
                        // Get the recommended tool
                        var recommendedTool = GetTool(intentResult.RecommendedTool);

                        if (recommendedTool != null)
                        {
                            _logger.LogInformation(
                                "Using NLP-recommended tool: {Tool} for query: '{Query}'",
                                recommendedTool.Name, query);

                            return recommendedTool;
                        }
                    }

                    // If we couldn't get a high-confidence recommendation, fall back to traditional matching
                    _logger.LogInformation(
                        "NLP confidence too low ({Confidence}), falling back to traditional tool matching",
                        intentResult.Confidence);
                }
                catch (Exception ex)
                {
                    // Log the error and fall back to traditional matching
                    _logger.LogError(ex, "Error using NLP for tool selection, falling back to traditional matching");
                }
            }

            // Fall back to traditional matching (checking each tool's CanHandle method)
            return FindBestToolForQuery(query);
        }

        /// <summary>
        /// Finds the best tool for a query using traditional matching
        /// </summary>
        public ITool FindBestToolForQuery(string query)
        {
            // First try specific tools that can confidently handle the query
            foreach (var tool in _tools.Where(t => t.Name != "Chat")) // Try all tools except Chat first
            {
                if (tool.CanHandle(query))
                {
                    _logger.LogInformation("Selected tool {Tool} for query: '{Query}'", tool.Name, query);
                    return tool;
                }
            }

            // If no specific tool was found, fall back to Chat tool
            var chatTool = _tools.FirstOrDefault(t => t.Name == "Chat");
            if (chatTool != null)
            {
                _logger.LogInformation("Falling back to Chat tool for query: '{Query}'", query);
                return chatTool;
            }

            // If even the Chat tool isn't available, return the first tool
            _logger.LogWarning("No suitable tool found for query: '{Query}', using first available tool", query);
            return _tools.First();
        }
    }
}