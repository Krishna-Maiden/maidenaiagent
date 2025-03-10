using Microsoft.Extensions.Logging;
using MaidenAIAgent.Core.Models;
using MaidenAIAgent.Core.Tools;
using MaidenAIAgent.Shared.Services;

namespace MaidenAIAgent.Core.Services
{
    /// <summary>
    /// Enhanced agent service that uses NLP for intent recognition and entity extraction
    /// </summary>
    public class EnhancedAgentService : IAgentService
    {
        private readonly IToolRegistry _toolRegistry;
        private readonly INLPService? _nlpService;
        private readonly ILogger<EnhancedAgentService> _logger;

        public EnhancedAgentService(
            IToolRegistry toolRegistry,
            ILogger<EnhancedAgentService> logger,
            INLPService? nlpService = null)
        {
            _toolRegistry = toolRegistry;
            _nlpService = nlpService;
            _logger = logger;
        }

        /// <summary>
        /// Processes a user query with NLP-enhanced understanding
        /// </summary>
        public async Task<AgentResponse> ProcessQueryAsync(AgentRequest request)
        {
            try
            {
                ITool tool;
                Dictionary<string, string> enhancedParameters = new(request.Parameters);

                // Extract entities and parameters if NLP service is available
                if (_nlpService != null)
                {
                    try
                    {
                        var entitiesResult = await _nlpService.ExtractEntitiesAsync(request.Query);

                        // Add extracted parameters to the request parameters
                        foreach (var param in entitiesResult.Parameters)
                        {
                            if (!enhancedParameters.ContainsKey(param.Key))
                            {
                                enhancedParameters[param.Key] = param.Value;
                                _logger.LogInformation(
                                    "Added extracted parameter {Key}={Value} from query",
                                    param.Key, param.Value);
                            }
                        }

                        // Add sentiment analysis if useful
                        var sentimentResult = await _nlpService.AnalyzeSentimentAsync(request.Query);
                        if (sentimentResult.Score > 0.7) // Fixed: Only add if confidence is high (changed from 7 to 0.7)
                        {
                            enhancedParameters["sentiment"] = sentimentResult.Sentiment;

                            // Add urgency if it's high
                            if (sentimentResult.Attributes.TryGetValue("urgency", out var urgency) && urgency > 0.7)
                            {
                                enhancedParameters["urgency"] = "high";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with regular processing
                        _logger.LogError(ex, "Error extracting entities and sentiment, continuing with regular processing");
                    }
                }

                // Find the right tool to handle the query
                if (request.UseAllTools)
                {
                    // Use the enhanced async version that leverages NLP
                    tool = await _toolRegistry.FindBestToolForQueryAsync(request.Query);
                }
                else if (request.SpecificTools.Any())
                {
                    tool = _toolRegistry.GetTool(request.SpecificTools.First());
                }
                else
                {
                    return new AgentResponse
                    {
                        Success = false,
                        ErrorMessage = "No suitable tool found for the query"
                    };
                }

                if (tool == null)
                {
                    return new AgentResponse
                    {
                        Success = false,
                        Response = "I don't know how to handle this query. Please try something else.",
                        ErrorMessage = "No suitable tool found"
                    };
                }

                // Execute the selected tool with enhanced parameters
                var result = await tool.ExecuteAsync(request.Query, enhancedParameters);

                return new AgentResponse
                {
                    Success = result.Success,
                    Response = result.Result,
                    ToolUsed = tool.Name,
                    Data = result.Data,
                    ErrorMessage = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query: {Query}", request.Query);

                return new AgentResponse
                {
                    Success = false,
                    ErrorMessage = $"Error processing query: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets information about available tools
        /// </summary>
        public IEnumerable<ToolInfo> GetAvailableTools()
        {
            return _toolRegistry.GetAllTools().Select(t => new ToolInfo
            {
                Name = t.Name,
                Description = t.Description
            });
        }
    }
}