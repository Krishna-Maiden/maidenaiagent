using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MaidenAIAgent.Shared.Services;

namespace MaidenAIAgent.Infrastructure.Services
{
    public class ClaudeSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public string ModelName { get; set; } = "claude-3-7-sonnet-20250219";
        public int MaxTokens { get; set; } = 1024;
        public double Temperature { get; set; } = 0.7;
        public string DefaultSystemPrompt { get; set; } = "You are Claude, an AI assistant created by Anthropic. You are helpful, harmless, and honest.";
    }

    public class ClaudeService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly ClaudeSettings _settings;
        private const string API_VERSION = "2023-06-01";

        public ClaudeService(HttpClient httpClient, IOptions<ClaudeSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", API_VERSION);
        }

        public async Task<LLMResponse> GenerateResponseAsync(string query, string? context = null, string? systemPrompt = null)
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
                    Temperature = _settings.Temperature
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
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = $"Exception calling Claude API: {ex.Message}"
                };
            }
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

        #endregion
    }
}