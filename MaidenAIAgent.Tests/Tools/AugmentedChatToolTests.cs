using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaidenAIAgent.Core.Services;
using MaidenAIAgent.Core.Tools;
using MaidenAIAgent.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MaidenAIAgent.Tests.Tools
{
    public class AugmentedChatToolTests
    {
        [Fact]
        public async Task ExecuteAsync_WithSimpleQuery_ReturnsDirectResponse()
        {
            // Arrange
            var mockLLMService = new Mock<ILLMService>();
            var mockOrchestrator = new Mock<IToolOrchestratorService>();
            var mockLogger = new Mock<ILogger<AugmentedChatTool>>();
            var mockSettings = Options.Create(new ChatToolSettings());

            var tool = new AugmentedChatTool(
                mockLLMService.Object,
                mockOrchestrator.Object,
                mockSettings,
                mockLogger.Object);

            // Act
            var result = await tool.ExecuteAsync("hello", new Dictionary<string, string>());

            // Assert
            Assert.True(result.Success);
            Assert.False((bool)result.Data["used_llm"]);
            Assert.False((bool)result.Data["used_tools"]);
            Assert.Contains("Hello", result.Result);

            // Verify LLM service was not called
            mockLLMService.Verify(s => s.GenerateResponseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoToolRequest_ReturnsLLMResponse()
        {
            // Arrange
            var mockLLMService = new Mock<ILLMService>();
            var mockOrchestrator = new Mock<IToolOrchestratorService>();
            var mockLogger = new Mock<ILogger<AugmentedChatTool>>();
            var mockSettings = Options.Create(new ChatToolSettings());

            // Set up orchestrator to return available tools
            mockOrchestrator.Setup(o => o.GetAllTools()).Returns(new List<ToolInfo>
            {
                new ToolInfo { Name = "Calculator", Description = "Performs calculations" },
                new ToolInfo { Name = "Weather", Description = "Gets weather information" }
            });

            // Set up LLM service to return a response without tool requests
            mockLLMService.Setup(s => s.GenerateResponseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(new LLMResponse
                {
                    Success = true,
                    Content = "This is a test response without tool requests.",
                    TokensUsed = 10
                });

            var tool = new AugmentedChatTool(
                mockLLMService.Object,
                mockOrchestrator.Object,
                mockSettings,
                mockLogger.Object);

            // Act
            var result = await tool.ExecuteAsync("What is the meaning of life?", new Dictionary<string, string>());

            // Assert
            Assert.True(result.Success);
            Assert.True((bool)result.Data["used_llm"]);
            Assert.False((bool)result.Data["used_tools"]);
            Assert.Equal("This is a test response without tool requests.", result.Result);
        }

        [Fact]
        public async Task ExecuteAsync_WithToolRequest_UsesTool()
        {
            // Arrange
            var mockLLMService = new Mock<ILLMService>();
            var mockOrchestrator = new Mock<IToolOrchestratorService>();
            var mockLogger = new Mock<ILogger<AugmentedChatTool>>();
            var mockSettings = Options.Create(new ChatToolSettings());

            // Set up orchestrator to return available tools
            mockOrchestrator.Setup(o => o.GetAllTools()).Returns(new List<ToolInfo>
            {
                new ToolInfo { Name = "Calculator", Description = "Performs calculations" }
            });

            // Set up orchestrator to execute Calculator tool
            mockOrchestrator.Setup(o => o.ExecuteToolAsync(
                "Calculator",
                "2+2",
                It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new ToolResult
                {
                    Success = true,
                    Result = "4"
                });

            // Set up LLM service to return a response with a tool request
            mockLLMService.Setup(s => s.GenerateResponseAsync(
                It.Is<string>(q => q.Contains("What is 2+2?")),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(new LLMResponse
                {
                    Success = true,
                    Content = "Let me calculate that for you.\n<tool name=\"Calculator\">\n2+2\n</tool>",
                    TokensUsed = 10
                });

            // Set up LLM service to handle the follow-up with tool results
            mockLLMService.Setup(s => s.GenerateResponseAsync(
                It.Is<string>(q => q.Contains("<tool_response>")),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(new LLMResponse
                {
                    Success = true,
                    Content = "The result of 2+2 is 4.",
                    TokensUsed = 5
                });

            var tool = new AugmentedChatTool(
                mockLLMService.Object,
                mockOrchestrator.Object,
                mockSettings,
                mockLogger.Object);

            // Act
            var result = await tool.ExecuteAsync("What is 2+2?", new Dictionary<string, string>());

            // Assert
            Assert.True(result.Success);
            Assert.True((bool)result.Data["used_llm"]);
            Assert.True((bool)result.Data["used_tools"]);
            Assert.Equal("The result of 2+2 is 4.", result.Result);

            // Verify that the tool was executed
            mockOrchestrator.Verify(o => o.ExecuteToolAsync(
                "Calculator",
                "2+2",
                It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenLLMFails_ReturnsError()
        {
            // Arrange
            var mockLLMService = new Mock<ILLMService>();
            var mockOrchestrator = new Mock<IToolOrchestratorService>();
            var mockLogger = new Mock<ILogger<AugmentedChatTool>>();
            var mockSettings = Options.Create(new ChatToolSettings());

            // Set up LLM service to fail
            mockLLMService.Setup(s => s.GenerateResponseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "Test error"
                });

            var tool = new AugmentedChatTool(
                mockLLMService.Object,
                mockOrchestrator.Object,
                mockSettings,
                mockLogger.Object);

            // Act
            var result = await tool.ExecuteAsync("Complex query", new Dictionary<string, string>());

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to get response from Claude", result.ErrorMessage);
        }
    }
}