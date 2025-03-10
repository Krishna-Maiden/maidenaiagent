using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaidenAIAgent.Core.Models;
using MaidenAIAgent.Core.Services;
using MaidenAIAgent.Core.Tools;
using MaidenAIAgent.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MaidenAIAgent.Tests.Services
{
    public class EnhancedAgentServiceTests
    {
        [Fact]
        public async Task ProcessQueryAsync_WithHighSentimentScore_AddsSentimentParameter()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<EnhancedAgentService>>();

            // Create a mock of IToolRegistry instead of the concrete class
            var mockToolRegistry = new Mock<IToolRegistry>();

            // Create a mock for INLPService
            var mockNlpService = new Mock<INLPService>();

            // Create a mock tool that will capture the parameters
            var capturedParameters = new Dictionary<string, string>();
            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns("TestTool");
            mockTool.Setup(t => t.CanHandle(It.IsAny<string>())).Returns(true);
            mockTool.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((query, parameters) =>
                {
                    // Capture the parameters passed to the tool
                    foreach (var param in parameters)
                    {
                        capturedParameters[param.Key] = param.Value;
                    }
                })
                .ReturnsAsync(new ToolResult { Success = true, Result = "Test result" });

            // Setup the registry to return our mock tool
            mockToolRegistry.Setup(r => r.FindBestToolForQueryAsync(It.IsAny<string>()))
                .ReturnsAsync(mockTool.Object);

            // The key part of the test: Setup NLP service to return a high sentiment score (greater than 0.7)
            var sentimentResult = new SentimentAnalysisResult
            {
                Sentiment = "positive",
                Score = 0.8, // This is the score that should trigger the condition
                Attributes = new Dictionary<string, double>
                {
                    { "urgency", 0.9 } // This should trigger the urgency parameter
                }
            };

            // Setup mock NLP service
            mockNlpService.Setup(s => s.ExtractEntitiesAsync(It.IsAny<string>()))
                .ReturnsAsync(new EntityExtractionResult());
            mockNlpService.Setup(s => s.AnalyzeSentimentAsync(It.IsAny<string>()))
                .ReturnsAsync(sentimentResult);

            // Create the service under test
            var service = new EnhancedAgentService(
                mockToolRegistry.Object,
                mockLogger.Object,
                mockNlpService.Object);

            // Create the request
            var request = new AgentRequest
            {
                Query = "I need help immediately!",
                UseAllTools = true
            };

            // Act
            var result = await service.ProcessQueryAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("TestTool", result.ToolUsed);

            // Verify the sentiment parameter was added with the expected value
            Assert.True(capturedParameters.ContainsKey("sentiment"));
            Assert.Equal("positive", capturedParameters["sentiment"]);

            // Verify urgency parameter was added
            Assert.True(capturedParameters.ContainsKey("urgency"));
            Assert.Equal("high", capturedParameters["urgency"]);
        }

        [Fact]
        public async Task ProcessQueryAsync_WithLowSentimentScore_DoesNotAddSentimentParameter()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<EnhancedAgentService>>();

            // Create a mock of IToolRegistry instead of the concrete class
            var mockToolRegistry = new Mock<IToolRegistry>();

            // Create a mock for INLPService
            var mockNlpService = new Mock<INLPService>();

            // Create a mock tool that will capture the parameters
            var capturedParameters = new Dictionary<string, string>();
            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns("TestTool");
            mockTool.Setup(t => t.CanHandle(It.IsAny<string>())).Returns(true);
            mockTool.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((query, parameters) =>
                {
                    // Capture the parameters passed to the tool
                    foreach (var param in parameters)
                    {
                        capturedParameters[param.Key] = param.Value;
                    }
                })
                .ReturnsAsync(new ToolResult { Success = true, Result = "Test result" });

            // Setup the repository to return our mock tool
            mockToolRegistry.Setup(r => r.FindBestToolForQueryAsync(It.IsAny<string>()))
                .ReturnsAsync(mockTool.Object);

            // The key part of the test: Setup NLP service to return a low sentiment score (less than 0.7)
            var sentimentResult = new SentimentAnalysisResult
            {
                Sentiment = "neutral",
                Score = 0.5, // This is the score that should NOT trigger the condition
                Attributes = new Dictionary<string, double>
                {
                    { "urgency", 0.3 } // This should NOT trigger the urgency parameter
                }
            };

            // Setup mock NLP service
            mockNlpService.Setup(s => s.ExtractEntitiesAsync(It.IsAny<string>()))
                .ReturnsAsync(new EntityExtractionResult());
            mockNlpService.Setup(s => s.AnalyzeSentimentAsync(It.IsAny<string>()))
                .ReturnsAsync(sentimentResult);

            // Create the service under test
            var service = new EnhancedAgentService(
                mockToolRegistry.Object,
                mockLogger.Object,
                mockNlpService.Object);

            // Create the request
            var request = new AgentRequest
            {
                Query = "Can you help me with something?",
                UseAllTools = true
            };

            // Act
            var result = await service.ProcessQueryAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("TestTool", result.ToolUsed);

            // Verify the sentiment parameter was NOT added
            Assert.False(capturedParameters.ContainsKey("sentiment"));

            // Verify urgency parameter was NOT added
            Assert.False(capturedParameters.ContainsKey("urgency"));
        }
    }
}