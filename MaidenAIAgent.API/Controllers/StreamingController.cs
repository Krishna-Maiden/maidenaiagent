using System.Net;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using MaidenAIAgent.Core.Models;
using MaidenAIAgent.Core.Tools;
using MaidenAIAgent.Shared.Services;

namespace MaidenAIAgent.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamingController : ControllerBase
    {
        private readonly ITool _streamingChatTool;

        public StreamingController(IEnumerable<ITool> tools)
        {
            // Find the streaming chat tool by name
            _streamingChatTool = tools.FirstOrDefault(t => t.Name == "StreamingChat") ??
                                throw new InvalidOperationException("StreamingChat tool not registered");
        }

        /// <summary>
        /// Streams a response to a chat query
        /// </summary>
        [HttpPost("chat")]
        public async Task<IActionResult> StreamChat([FromBody] StreamingRequest request)
        {
            if (string.IsNullOrEmpty(request.Query))
            {
                return BadRequest("Query cannot be empty");
            }

            // Add streaming flag to parameters
            var parameters = request.Parameters ?? new Dictionary<string, string>();
            parameters["useStreaming"] = "true";

            // Execute the tool with streaming enabled
            var result = await _streamingChatTool.ExecuteAsync(request.Query, parameters);

            if (!result.Success)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new
                {
                    Error = result.ErrorMessage ?? "Unknown error processing streaming request"
                });
            }

            // Get the streaming channel from the result
            if (!result.Data.TryGetValue("streamingChannel", out var channelObj) ||
                channelObj is not ChannelReader<StreamingResponseChunk> channel)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new
                {
                    Error = "Streaming response channel not available"
                });
            }

            // Set the response content type for Server-Sent Events
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Disable buffering
            Response.Body.Flush();

            // Prepare a CancellationToken that will trigger if the client disconnects
            var clientDisconnectionToken = HttpContext.RequestAborted;

            // Stream the response chunks to the client
            try
            {
                await foreach (var chunk in channel.ReadAllAsync(clientDisconnectionToken))
                {
                    if (!string.IsNullOrEmpty(chunk.Error))
                    {
                        // Write error event
                        await WriteEvent("error", new { message = chunk.Error });
                    }
                    else if (!string.IsNullOrEmpty(chunk.Content))
                    {
                        // Write data event
                        await WriteEvent("data", chunk.Content);
                    }

                    if (chunk.IsComplete)
                    {
                        // Write completion event
                        await WriteEvent("done", new { });
                    }

                    // Ensure each event is sent immediately
                    await Response.Body.FlushAsync(clientDisconnectionToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected, nothing to do
            }
            catch (Exception ex)
            {
                // Try to send an error if possible
                try
                {
                    await WriteEvent("error", new { message = $"Error streaming response: {ex.Message}" });
                    await Response.Body.FlushAsync();
                }
                catch
                {
                    // Ignore further errors during error reporting
                }
            }

            return new EmptyResult();
        }

        /// <summary>
        /// Helper method to write server-sent events
        /// </summary>
        private async Task WriteEvent(string eventType, object data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            await Response.WriteAsync($"event: {eventType}\n");
            await Response.WriteAsync($"data: {json}\n\n");
        }
    }

    /// <summary>
    /// Request model for streaming endpoints
    /// </summary>
    public class StreamingRequest
    {
        /// <summary>
        /// The user's query
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Optional parameters to include with the request
        /// </summary>
        public Dictionary<string, string>? Parameters { get; set; }

        /// <summary>
        /// Optional timeout in seconds (defaults to system setting)
        /// </summary>
        public int? TimeoutSeconds { get; set; }
    }
}