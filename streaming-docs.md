# Streaming Response Documentation

## Overview

The MaidenAI Agent now supports streaming responses for long-form content, providing a more responsive and interactive user experience. This feature is particularly valuable for detailed explanations, creative writing, technical guidance, and other scenarios where responses may be lengthy.

## Streaming Architecture

The streaming implementation follows a Server-Sent Events (SSE) approach with these key components:

1. **IStreamingLLMService Interface**: Extends the ILLMService with streaming capabilities
2. **StreamingClaudeService**: Implements streaming support for Claude API
3. **StreamingChatTool**: Tool that supports both standard and streaming responses
4. **StreamingController**: API endpoint for streaming responses

## How Streaming Works

The streaming process follows this flow:

1. **Client Request**: The client makes a request to the streaming endpoint
2. **Stream Initialization**: The server initializes a connection with Claude API in streaming mode
3. **Chunk Processing**: As Claude generates content, it's sent in small chunks to the client
4. **Progressive Rendering**: The client can render these chunks progressively
5. **Connection Management**: The stream continues until completion or disconnection

## API Usage

### Streaming Chat Endpoint

```http
POST /api/streaming/chat
Content-Type: application/json

{
  "query": "Write a detailed explanation of quantum computing",
  "parameters": {
    "context": "The user is a software developer with basic physics knowledge"
  },
  "timeoutSeconds": 120
}
```

### Response Format

The response is a series of Server-Sent Events (SSE):

```
event: data
data: "Quantum computing is a type of computing that"

event: data
data: " uses quantum mechanics to perform operations on data."

...

event: done
data: {}
```

### Event Types

- **data**: Contains a chunk of the response text
- **error**: Indicates an error occurred during streaming
- **done**: Signals the end of the response

## Implementation Details

### Streaming Detection

The system automatically detects when streaming is appropriate:

1. **Explicit Request**: Client can request streaming via the `useStreaming` parameter
2. **Query Analysis**: Long-form indicators in the query (e.g., "explain in detail")
3. **Query Length**: Longer queries (>100 characters) often need detailed responses
4. **Multiple Questions**: Queries with multiple question marks

### Streaming Management

The implementation includes several features for robust streaming:

1. **Bounded Channels**: Buffer management to handle fast producers/slow consumers
2. **Cancellation Support**: Handling of client disconnections
3. **Timeout Handling**: Configurable timeouts to prevent long-running requests
4. **Error Reporting**: Structured error events for client notification

### Client Integration

To integrate with the streaming API, clients should:

1. Establish an EventSource connection to the streaming endpoint
2. Process events as they arrive
3. Render content incrementally
4. Handle error and completion events

Example JavaScript client:

```javascript
const eventSource = new EventSource('/api/streaming/chat');

eventSource.addEventListener('data', (event) => {
  const chunk = JSON.parse(event.data);
  // Append the chunk to the displayed response
  appendToResponse(chunk);
});

eventSource.addEventListener('error', (event) => {
  const error = JSON.parse(event.data);
  showError(error.message);
  eventSource.close();
});

eventSource.addEventListener('done', () => {
  // Response is complete
  markResponseComplete();
  eventSource.close();
});
```

## Configuration

The streaming functionality can be configured in `appsettings.json`:

```json
"ChatToolSettings": {
  "ResponseTimeoutSeconds": 60,
  "DefaultToStreaming": true
}
```

### Settings Explained

- **ResponseTimeoutSeconds**: Maximum time a streaming response can take
- **DefaultToStreaming**: Whether to use streaming by default for long-form responses

## Performance Considerations

1. **Connection Management**: Each streaming connection consumes server resources
2. **Rate Limiting**: Streaming requests still count toward rate limits
3. **Client Resources**: Ensure clients can handle continuous updates

## Fallback Behavior

If streaming fails or is not available, the system automatically falls back to standard non-streaming responses. This ensures compatibility with all clients while providing enhanced experience where supported.

## Example Use Cases

1. **Technical Tutorials**: Step-by-step guides that may be lengthy
2. **Creative Writing**: Stories or articles that benefit from progressive display
3. **Data Analysis Reports**: Detailed analysis of complex datasets
4. **Educational Content**: In-depth explanations of complex topics

## Future Enhancements

1. **Bi-directional Streaming**: Allow clients to stream inputs for real-time conversations
2. **Progress Indicators**: Provide completion percentage estimates
3. **Token Counting**: Live token usage tracking
4. **Pause/Resume**: Allow users to control the flow of streaming
