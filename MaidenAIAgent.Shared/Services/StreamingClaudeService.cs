using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaidenAIAgent.Shared.Services;
using MaidenAIAgent.Infrastructure.Services;

namespace MaidenAIAgent.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IStreamingLLMService that connects to Claude API with streaming support
    /// </summary>
    public class StreamingClaudeService : IStreamingLLMService
    {
        private readonly HttpClient _httpClient;
        private readonly ClaudeSettings _settings;
        private readonly ILogger<StreamingClaudeService> _logger;
        private const string API_VERSION = "2023-06-01";
        private const int CHANNEL_CAPACITY = 100; // Buffer size for streaming chunks

        public StreamingClaudeService(
            HttpClient httpClient,
            IOptions<ClaudeSettings> settings,
            ILogger<StreamingClaudeService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", API_VERSION);
        }

        /// <summary>
        /// Generates a non-streaming response from Claude
        /// </summary>
        public async Task<LLMResponse> GenerateResponseAsync(
            string query,
            string? context = null,
            string? systemPrompt = null)
        {
            try
            {
                // Prepare message content
                var messages = new List<Message>
                {
                    new Message { Role = "user", Content = context != null ? $"{context}\n\n{query}" : query }
                };

                // Prepare the request
                var request = new ClaudeRequest
                {
                    Model = _settings.ModelName,
                    Messages = messages,
                    System = systemPrompt ?? _settings.DefaultSystemPrompt,
                    MaxTokens = _settings.MaxTokens,
                    Temperature = _settings.Temperature,
                    Stream = false // Ensure non-streaming request
                };

                // Send the request to Claude API
                var response = await _httpClient.PostAsJsonAsync("/v1/messages", request);

                // Check if request was successful
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = $"Error from Claude API: {response.StatusCode}. {errorContent}"
                    };
                }

                // Parse the response
                var claudeResponse = await response.Content.ReadFromJsonAsync<ClaudeResponse>();

                if (claudeResponse == null)
                {
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse response from Claude API"
                    };
                }

                // Extract the assistant's message
                var content = claudeResponse.Content.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty;

                return new LLMResponse
                {
                    Content = content,
                    Success = true,
                    TokensUsed = claudeResponse.Usage.OutputTokens,
                    Metadata = new Dictionary<string, object>
                    {
                        { "model", claudeResponse.Model },
                        { "input_tokens", claudeResponse.Usage.InputTokens },
                        { "output_tokens", claudeResponse.Usage.OutputTokens },
                        { "id", claudeResponse.Id },
                        { "type", claudeResponse.Type }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Claude API: {Message}", ex.Message);
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = $"Exception calling Claude API: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Generates a streaming response from Claude
        /// </summary>
        public async Task<ChannelReader<StreamingResponseChunk>> GenerateStreamingResponseAsync(
            string query,
            string? context = null,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            // Create a channel to communicate streaming chunks
            var channel = Channel.CreateBounded<StreamingResponseChunk>(new BoundedChannelOptions(CHANNEL_CAPACITY)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            // Start a background task to stream responses
            _ = Task.Run(async () =>
            {
                try
                {
                    // Prepare message content
                    var messages = new List<Message>
                    {
                        new Message { Role = "user", Content = context != null ? $"{context}\n\n{query}" : query }
                    };

                    // Prepare the request
                    var request = new ClaudeRequest
                    {
                        Model = _settings.ModelName,
                        Messages = messages,
                        System = systemPrompt ?? _settings.DefaultSystemPrompt,
                        MaxTokens = _settings.MaxTokens,
                        Temperature = _settings.Temperature,
                        Stream = true // Enable streaming
                    };

                    // Serialize the request manually
                    var requestJson = JsonSerializer.Serialize(request);
                    var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    // Send the request with the appropriate headers for streaming
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
                    {
                        Content = requestContent
                    };

                    // Send the request to Claude API
                    using var response = await _httpClient.SendAsync(
                        httpRequest,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    // Check if request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        await channel.Writer.WriteAsync(new StreamingResponseChunk
                        {
                            Error = $"Error from Claude API: {response.StatusCode}. {errorContent}",
                            IsComplete = true
                        }, cancellationToken);

                        channel.Writer.Complete();
                        return;
                    }

                    // Get the stream from the response
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var reader = new StreamReader(stream);

                    var fullResponse = new StringBuilder();

                    // Read the stream line by line
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                    {
                        // Skip empty lines
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        // SSE format starts with "data: "
                        if (line.StartsWith("data: "))
                        {
                            var jsonData = line.Substring("data: ".Length);

                            // [DONE] indicates end of stream
                            if (jsonData == "[DONE]")
                            {
                                await channel.Writer.WriteAsync(new StreamingResponseChunk
                                {
                                    Content = string.Empty,
                                    IsComplete = true
                                }, cancellationToken);
                                break;
                            }

                            try
                            {
                                // Parse the streaming response chunk
                                var chunkResponse = JsonSerializer.Deserialize<ClaudeStreamingResponse>(jsonData);

                                if (chunkResponse != null &&
                                    chunkResponse.Type == "content_block_delta" &&
                                    chunkResponse.Delta?.Type == "text_delta")
                                {
                                    var textDelta = chunkResponse.Delta.Text;

                                    if (!string.IsNullOrEmpty(textDelta))
                                    {
                                        fullResponse.Append(textDelta);

                                        await channel.Writer.WriteAsync(new StreamingResponseChunk
                                        {
                                            Content = textDelta,
                                            IsComplete = false,
                                            Metadata = new Dictionary<string, object>
                                            {
                                                { "model", chunkResponse.Model ?? "unknown" },
                                                { "type", chunkResponse.Type },
                                                { "index", chunkResponse.Index }
                                            }
                                        }, cancellationToken);
                                    }
                                }
                                else if (chunkResponse?.Type == "message_stop")
                                {
                                    // Message completion
                                    _logger.LogInformation("Claude streaming response completed");
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Error parsing Claude streaming response JSON: {Json}", jsonData);

                                await channel.Writer.WriteAsync(new StreamingResponseChunk
                                {
                                    Error = $"Error parsing Claude streaming response: {ex.Message}",
                                    IsComplete = true
                                }, cancellationToken);
                                break;
                            }
                        }
                    }

                    // Ensure we mark completion if we exited the loop normally
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await channel.Writer.WriteAsync(new StreamingResponseChunk
                        {
                            Content = string.Empty,
                            IsComplete = true
                        }, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Claude streaming: {Message}", ex.Message);

                    // Try to write the error to the channel
                    try
                    {
                        await channel.Writer.WriteAsync(new StreamingResponseChunk
                        {
                            Error = $"Exception in Claude streaming: {ex.Message}",
                            IsComplete = true
                        }, cancellationToken);
                    }
                    catch (Exception channelEx)
                    {
                        _logger.LogError(channelEx, "Error writing error to channel: {Message}", channelEx.Message);
                    }
                }
                finally
                {
                    // Always mark the channel as complete
                    channel.Writer.Complete();
                }
            }, cancellationToken);

            return channel.Reader;
        }

        #region Claude API Models

        private class ClaudeRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public List<Message> Messages { get; set; } = new List<Message>();

            [JsonPropertyName("system")]
            public string System { get; set; } = string.Empty;

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }
        }

        private class Message
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private class ClaudeResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public List<Content> Content { get; set; } = new List<Content>();

            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("usage")]
            public Usage Usage { get; set; } = new Usage();
        }

        private class Content
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        private class Usage
        {
            [JsonPropertyName("input_tokens")]
            public int InputTokens { get; set; }

            [JsonPropertyName("output_tokens")]
            public int OutputTokens { get; set; }
        }

        private class ClaudeStreamingResponse
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("delta")]
            public Delta? Delta { get; set; }

            [JsonPropertyName("model")]
            public string? Model { get; set; }
        }

        private class Delta
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        #endregion
    }
}