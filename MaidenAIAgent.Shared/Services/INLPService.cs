namespace MaidenAIAgent.Shared.Services
{
    /// <summary>
    /// Interface for NLP (Natural Language Processing) services 
    /// </summary>
    public interface INLPService
    {
        /// <summary>
        /// Classifies the intent of a user query
        /// </summary>
        /// <param name="query">The user's query text</param>
        /// <returns>Intent classification result</returns>
        Task<IntentClassificationResult> ClassifyIntentAsync(string query);

        /// <summary>
        /// Extracts entities from the user query
        /// </summary>
        /// <param name="query">The user's query text</param>
        /// <returns>Entity extraction result</returns>
        Task<EntityExtractionResult> ExtractEntitiesAsync(string query);

        /// <summary>
        /// Analyzes the sentiment of the user query
        /// </summary>
        /// <param name="query">The user's query text</param>
        /// <returns>Sentiment analysis result</returns>
        Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string query);
    }

    /// <summary>
    /// Represents the result of intent classification
    /// </summary>
    public class IntentClassificationResult
    {
        /// <summary>
        /// The primary intent identified in the query
        /// </summary>
        public string PrimaryIntent { get; set; } = string.Empty;

        /// <summary>
        /// The confidence score for the primary intent (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// All identified intents with their confidence scores
        /// </summary>
        public Dictionary<string, double> AllIntents { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// The recommended tool to handle this intent
        /// </summary>
        public string RecommendedTool { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the result of entity extraction
    /// </summary>
    public class EntityExtractionResult
    {
        /// <summary>
        /// Extracted named entities and their types
        /// </summary>
        public Dictionary<string, string> Entities { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Parameters that can be used by tools, extracted from the query
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents the result of sentiment analysis
    /// </summary>
    public class SentimentAnalysisResult
    {
        /// <summary>
        /// The overall sentiment (positive, negative, neutral)
        /// </summary>
        public string Sentiment { get; set; } = string.Empty;

        /// <summary>
        /// The score for the identified sentiment (0.0 to 1.0)
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Additional sentiment attributes (urgency, frustration, satisfaction, etc.)
        /// </summary>
        public Dictionary<string, double> Attributes { get; set; } = new Dictionary<string, double>();
    }
}