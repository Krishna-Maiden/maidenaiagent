# Chat Tool Documentation

## Overview

The Chat Tool is designed to handle conversational queries and provide intelligent responses using Claude 3.7 Sonnet, Anthropic's large language model. It serves as both a specialized tool for simple interactions and a fallback for general queries that other tools cannot handle.

## Features

- Integration with Claude 3.7 Sonnet for sophisticated, natural language responses
- Fast handling of simple queries with predefined responses
- Fallback capability for general questions not covered by other tools
- Configurable system prompts to customize Claude's behavior
- Performance optimization with optional response caching

## Architecture

### Components

1. **ChatTool**: The main tool class that handles conversational queries
2. **ILLMService**: Interface for language model integration
3. **ClaudeService**: Implementation of ILLMService for Claude API
4. **ChatToolSettings**: Configuration settings for the Chat Tool

### Integration Flow

1. The `CanHandle` method determines if a query should be handled by the Chat Tool
2. For simple queries (greetings, thanks), predefined responses are used
3. For complex queries, the request is forwarded to Claude 3.7 Sonnet
4. The response is formatted and returned to the agent

## Configuration

The Chat Tool can be configured through the `appsettings.json` file in the `ChatToolSettings` section:

```json
"ChatToolSettings": {
  "SystemPrompt": "You are a helpful AI assistant integrated into an AI Agent system...",
  "ResponseMaxLength": 500,
  "UseCache": true,
  "CacheExpirationMinutes": 60
}
```

Claude API settings can be configured in the `ClaudeSettings` section:

```json
"ClaudeSettings": {
  "ApiKey": "YOUR_ANTHROPIC_API_KEY",
  "BaseUrl": "https://api.anthropic.com",
  "ModelName": "claude-3-7-sonnet-20250219",
  "MaxTokens": 1024,
  "Temperature": 0.7,
  "DefaultSystemPrompt": "You are Claude, an AI assistant created by Anthropic..."
}
```

## Usage Examples

### Example 1: Simple Greeting

**Request:**
```json
{
  "query": "Hello there!",
  "useAllTools": true
}
```

**Response:**
```json
{
  "response": "Hello! How can I assist you today?",
  "toolUsed": "Chat",
  "data": {
    "query": "Hello there!",
    "responseType": "simple",
    "used_llm": false
  },
  "success": true
}
```

### Example 2: Complex Question (Using Claude)

**Request:**
```json
{
  "query": "What are the key considerations when implementing an AI agent architecture?",
  "useAllTools": true
}
```

**Response:**
```json
{
  "response": "When implementing an AI agent architecture, there are several key considerations to keep in mind...",
  "toolUsed": "Chat",
  "data": {
    "query": "What are the key considerations when implementing an AI agent architecture?",
    "responseType": "complex",
    "used_llm": true,
    "tokens_used": 145,
    "model": "claude-3-7-sonnet-20250219"
  },
  "success": true
}
```

## Implementation Details

### Claude API Integration

The service connects to the Anthropic API using:
- API Key authentication
- JSON request/response format
- Configurable model parameters (temperature, max tokens)

### Query Handling Logic

The Chat Tool decides how to handle queries based on:
1. **Specific Tool Check**: Determines if another tool should handle the query
2. **Simple Query Check**: Identifies if the query can be handled with a predefined response
3. **Fallback to Claude**: Uses Claude for complex or general queries

### Performance Optimization

For production deployments, consider:
- Implementing the optional caching mechanism
- Adjusting token limits based on anticipated query complexity
- Setting appropriate temperature values for consistent responses

## Security Notes

- The Claude API key should be stored securely in a vault or environment variable
- Consider implementing rate limiting to prevent excessive API usage
- Monitor token usage to manage costs
- Validate and sanitize input to prevent potential prompt injection

## Finished Enhancements

1. **Rate Limits**: To Prevent excessive API usage
2. **Improved Intent Recognition**: Integrate with NLP services for more accurate intent classification

## Future Enhancements

The Chat Tool can be extended in several ways:

1. **Contextual Memory**: Add session state to remember previous interactions
2. **Personalization**: Customize responses based on user preferences or history
3. **Multi-turn Conversations**: Support more complex dialog flows
4. **Conversation Memory**: Add session state to track conversation history
5. **Streaming Responses**: Implement streaming for long-form responses
6. **Tool Augmentation**: Allow Claude to use other tools as needed
7. **Prompt Engineering**: Refine system prompts for higher quality responses
8. **Multi-modal Support**: Add image understanding capabilities if needed