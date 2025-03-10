# NLP Integration Documentation

## Overview

The MaidenAI Agent has been enhanced with Natural Language Processing (NLP) capabilities to provide more accurate intent classification, entity extraction, and sentiment analysis. This integration improves the agent's ability to understand user queries and route them to the appropriate tool.

## Architecture

The NLP integration follows a service-oriented approach with these key components:

1. **INLPService Interface**: Defines the contract for NLP services
2. **ClaudeNLPService**: Implements NLP capabilities using Claude LLM
3. **EnhancedToolRegistry**: Uses NLP for better tool selection
4. **EnhancedAgentService**: Leverages extracted entities and sentiment

## Features

### Intent Classification

Intent classification identifies the user's purpose or goal in their query. The system:

- Analyzes the query text to determine the most likely intent
- Provides confidence scores for possible intents
- Recommends the most appropriate tool to handle the request
- Falls back to traditional pattern matching when confidence is low

### Entity Extraction

Entity extraction identifies specific pieces of information in the query:

- Named entities (people, places, organizations, dates, etc.)
- Tool-specific parameters (location for weather, expression for calculator)
- Converts extracted entities into usable parameters for tools

### Sentiment Analysis

Sentiment analysis determines the emotional tone of the query:

- Classifies sentiment as positive, negative, or neutral
- Measures attributes like urgency, frustration, and satisfaction
- Helps tools adapt responses based on user sentiment

## Implementation Details

### ClaudeNLPService

This service uses specialized system prompts to direct Claude to perform NLP tasks:

1. **Intent Classification Prompt**: Directs Claude to analyze intent and recommend a tool
2. **Entity Extraction Prompt**: Instructs Claude to identify entities and parameters
3. **Sentiment Analysis Prompt**: Guides Claude to assess sentiment and emotional attributes

All responses are structured as JSON for easy parsing and integration.

### Enhanced Tool Selection

The `EnhancedToolRegistry` improves tool selection by:

1. Using NLP intent classification when confidence exceeds a threshold (default: 65%)
2. Falling back to traditional pattern matching when NLP confidence is low
3. Providing a consistent interface compatible with the original implementation

### Parameter Enhancement

The `EnhancedAgentService` improves request handling by:

1. Extracting entities and parameters from queries automatically
2. Adding sentiment information when confidence is high
3. Passing enhanced parameters to tools for better responses

## Configuration

No additional configuration is required for basic NLP functionality, as it uses the existing Claude integration. However, you can adjust these settings:

- Confidence threshold for intent classification (in EnhancedToolRegistry)
- System prompts for different NLP tasks (in ClaudeNLPService)
- Sentiment threshold for including sentiment data (in EnhancedAgentService)

## Example Flow

1. User submits the query: "What's the temperature in Seattle tomorrow?"
2. The `EnhancedAgentService` receives the query
3. The `ClaudeNLPService` performs:
   - Intent classification: "weather" (confidence: 0.92)
   - Entity extraction: {"location": "Seattle", "time": "tomorrow"}
   - Sentiment analysis: "neutral" (not used due to low relevance)
4. The `EnhancedToolRegistry` selects the Weather tool
5. The Weather tool receives the query with enhanced parameters
6. The tool returns weather information for Seattle

## Benefits

This NLP integration provides several advantages:

1. **Improved Accuracy**: Better understanding of user intent
2. **Reduced Development Effort**: Less need for complex regex patterns
3. **Enhanced Context**: More parameters available to tools
4. **Adaptability**: Responses can be tailored based on sentiment
5. **Graceful Degradation**: Falls back to traditional methods when needed

## Future Enhancements

The NLP integration can be extended in several ways:

1. **Caching**: Store common intent classifications to reduce API calls
2. **Fine-tuning**: Train a specialized model for intent classification
3. **Feedback Loop**: Learn from successful and unsuccessful classifications
4. **Multi-turn Context**: Maintain conversation context across queries
5. **Entity Normalization**: Standardize extracted entities (e.g., date formats)

## Implementation Notes

- The Claude API is used for all NLP tasks
- Rate limiting applies to all NLP operations
- NLP processing adds latency but improves accuracy
- The system is designed to gracefully handle NLP failures
