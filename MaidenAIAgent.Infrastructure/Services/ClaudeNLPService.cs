using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaidenAIAgent.Shared.Services;
using System.Text.Json;

namespace MaidenAIAgent.Infrastructure.Services
{
    /// <summary>
    /// Implementation of INLPService that uses Claude for NLP tasks
    /// </summary>
    public class ClaudeNLPService : INLPService
    {
        private readonly ILLMService _claudeService;
        private readonly ILogger<ClaudeNLPService> _logger;

        // System prompts for different NLP tasks
        private const string INTENT_CLASSIFICATION_PROMPT = @"
You are an intent classification system. Analyze the user's query and determine their intent.
Respond with valid JSON only following this exact format:
{
  ""primaryIntent"": ""<main intent>"",
  ""confidence"": <confidence score from 0.0 to 1.0>,
  ""allIntents"": {
    ""<intent1>"": <score1>,
    ""<intent2>"": <score2>,
    ...
  },
  ""recommendedTool"": ""<tool name>""
}

Available tools are:
- ""Search"": For finding information on the internet
- ""Calculator"": For mathematical calculations
- ""Weather"": For weather information
- ""Chat"": For general conversation

Possible intents include:
- ""search"": User wants to find information
- ""calculate"": User wants to perform a calculation
- ""weather"": User wants weather information
- ""greeting"": User is saying hello
- ""help"": User wants assistance
- ""smalltalk"": User is making casual conversation
- ""clarification"": User wants clarification on something
- ""farewell"": User is saying goodbye
- ""feedback"": User is providing feedback

NOTE: Return ONLY the JSON in your response, no additional text.";

        private const string ENTITY_EXTRACTION_PROMPT = @"
You are an entity extraction system. Analyze the user's query and extract named entities and parameters.
Respond with valid JSON only following this exact format:
{
  ""entities"": {
    ""<entity text>"": ""<entity type>"",
    ...
  },
  ""parameters"": {
    ""<param name>"": ""<param value>"",
    ...
  }
}

Entity types include: PERSON, LOCATION, ORGANIZATION, DATE, TIME, QUANTITY, PRODUCT.
Parameter names should match what tools would need (e.g., ""location"" for weather, ""expression"" for calculator).

NOTE: Return ONLY the JSON in your response, no additional text.";

        private const string SENTIMENT_ANALYSIS_PROMPT = @"
You are a sentiment analysis system. Analyze the user's query and determine the sentiment.
Respond with valid JSON only following this exact format:
{
  ""sentiment"": ""<positive/negative/neutral>"",
  ""score"": <sentiment score from 0.0 to 1.0>,
  ""attributes"": {
    ""urgency"": <0.0 to 1.0>,
    ""frustration"": <0.0 to 1.0>,
    ""satisfaction"": <0.0 to 1.0>,
    ""confusion"": <0.0 to 1.0>,
    ""politeness"": <0.0 to 1.0>
  }
}

NOTE: Return ONLY the JSON in your response, no additional text.";

        public ClaudeNLPService(ILLMService claudeService, ILogger<ClaudeNLPService> logger)
        {
            _claudeService = claudeService;
            _logger = logger;
        }

        /// <summary>
        /// Classifies the intent of a user query
        /// </summary>
        public async Task<IntentClassificationResult> ClassifyIntentAsync(string query)
        {
            try
            {
                var response = await _claudeService.GenerateResponseAsync(query, null, INTENT_CLASSIFICATION_PROMPT);

                if (!response.Success)
                {
                    _logger.LogError("Failed to classify intent: {Error}", response.ErrorMessage);
                    return new IntentClassificationResult
                    {
                        PrimaryIntent = "unknown",
                        Confidence = 0,
                        RecommendedTool = "Chat" // Fallback to Chat tool
                    };
                }

                // Parse the JSON response
                var result = ParseIntentJson(response.Content);

                _logger.LogInformation("Intent classified: {Intent} with confidence {Confidence}, recommended tool: {Tool}",
                    result.PrimaryIntent, result.Confidence, result.RecommendedTool);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during intent classification");
                return new IntentClassificationResult
                {
                    PrimaryIntent = "unknown",
                    Confidence = 0,
                    RecommendedTool = "Chat" // Fallback to Chat tool
                };
            }
        }

        /// <summary>
        /// Extracts entities from the user query
        /// </summary>
        public async Task<EntityExtractionResult> ExtractEntitiesAsync(string query)
        {
            try
            {
                var response = await _claudeService.GenerateResponseAsync(query, null, ENTITY_EXTRACTION_PROMPT);

                if (!response.Success)
                {
                    _logger.LogError("Failed to extract entities: {Error}", response.ErrorMessage);
                    return new EntityExtractionResult();
                }

                // Parse the JSON response
                var result = ParseEntityJson(response.Content);

                _logger.LogInformation("Entities extracted: {Count}", result.Entities.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during entity extraction");
                return new EntityExtractionResult();
            }
        }

        /// <summary>
        /// Analyzes the sentiment of the user query
        /// </summary>
        public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string query)
        {
            try
            {
                var response = await _claudeService.GenerateResponseAsync(query, null, SENTIMENT_ANALYSIS_PROMPT);

                if (!response.Success)
                {
                    _logger.LogError("Failed to analyze sentiment: {Error}", response.ErrorMessage);
                    return new SentimentAnalysisResult
                    {
                        Sentiment = "neutral",
                        Score = 0.5
                    };
                }

                // Parse the JSON response
                var result = ParseSentimentJson(response.Content);

                _logger.LogInformation("Sentiment analyzed: {Sentiment} with score {Score}",
                    result.Sentiment, result.Score);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sentiment analysis");
                return new SentimentAnalysisResult
                {
                    Sentiment = "neutral",
                    Score = 0.5
                };
            }
        }

        #region JSON Parsing Methods

        private IntentClassificationResult ParseIntentJson(string json)
        {
            try
            {
                // Clean the JSON string to handle possible markdown code block
                json = CleanJsonString(json);

                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                var result = new IntentClassificationResult
                {
                    PrimaryIntent = GetJsonString(root, "primaryIntent"),
                    Confidence = GetJsonDouble(root, "confidence"),
                    RecommendedTool = GetJsonString(root, "recommendedTool")
                };

                // Get all intents
                if (root.TryGetProperty("allIntents", out var intentsElement) &&
                    intentsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var intent in intentsElement.EnumerateObject())
                    {
                        result.AllIntents[intent.Name] = intent.Value.GetDouble();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing intent JSON: {Json}", json);
                return new IntentClassificationResult
                {
                    PrimaryIntent = "unknown",
                    Confidence = 0,
                    RecommendedTool = "Chat" // Fallback to Chat tool
                };
            }
        }

        private EntityExtractionResult ParseEntityJson(string json)
        {
            try
            {
                // Clean the JSON string to handle possible markdown code block
                json = CleanJsonString(json);

                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                var result = new EntityExtractionResult();

                // Get entities
                if (root.TryGetProperty("entities", out var entitiesElement) &&
                    entitiesElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var entity in entitiesElement.EnumerateObject())
                    {
                        result.Entities[entity.Name] = entity.Value.GetString() ?? string.Empty;
                    }
                }

                // Get parameters
                if (root.TryGetProperty("parameters", out var paramsElement) &&
                    paramsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var param in paramsElement.EnumerateObject())
                    {
                        result.Parameters[param.Name] = param.Value.GetString() ?? string.Empty;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing entity JSON: {Json}", json);
                return new EntityExtractionResult();
            }
        }

        private SentimentAnalysisResult ParseSentimentJson(string json)
        {
            try
            {
                // Clean the JSON string to handle possible markdown code block
                json = CleanJsonString(json);

                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                var result = new SentimentAnalysisResult
                {
                    Sentiment = GetJsonString(root, "sentiment"),
                    Score = GetJsonDouble(root, "score")
                };

                // Get attributes
                if (root.TryGetProperty("attributes", out var attrsElement) &&
                    attrsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var attr in attrsElement.EnumerateObject())
                    {
                        result.Attributes[attr.Name] = attr.Value.GetDouble();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing sentiment JSON: {Json}", json);
                return new SentimentAnalysisResult
                {
                    Sentiment = "neutral",
                    Score = 0.5
                };
            }
        }

        private string CleanJsonString(string json)
        {
            // Remove markdown code block indicators if present
            json = json.Trim();
            if (json.StartsWith("```json"))
            {
                json = json.Substring("```json".Length);
            }
            else if (json.StartsWith("```"))
            {
                json = json.Substring("```".Length);
            }

            if (json.EndsWith("```"))
            {
                json = json.Substring(0, json.Length - "```".Length);
            }

            return json.Trim();
        }

        private string GetJsonString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        private double GetJsonDouble(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.GetDouble();
            }
            return 0.0;
        }

        #endregion
    }
}