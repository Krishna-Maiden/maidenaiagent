using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaidenAIAgent.Core.Services;
using MaidenAIAgent.Core.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MaidenAIAgent.Tests.Services
{
    public class ToolOrchestratorTests
    {
        [Fact]
        public async Task ExecuteToolAsync_WhenToolExists_ReturnsToolResult()
        {
            // Arrange
            var mockToolRegistry = new Mock<IToolRegistry>();
            var mockLogger = new Mock<ILogger<ToolOrchestratorService>>();

            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns("TestTool");
            mockTool.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new ToolResult { Success = true, Result = "Test result" });

            mockToolRegistry.Setup(t => t.GetTool("TestTool")).Returns(mockTool.Object);

            var orchestrator = new ToolOrchestratorService(mockToolRegistry.Object, mockLogger.Object);

            // Act
            var result = await orchestrator.ExecuteToolAsync("TestTool", "Test query", new Dictionary<string, string>());

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Test result", result.Result);
            mockTool.Verify(t => t.ExecuteAsync("Test query", It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteToolAsync_WhenToolDoesNotExist_ReturnsError()
        {
            // Arrange
            var mockToolRegistry = new Mock<IToolRegistry>();
            var mockLogger = new Mock<ILogger<ToolOrchestratorService>>();

            mockToolRegistry.Setup(t => t.GetTool(It.IsAny<string>())).Returns((ITool)null);
            mockToolRegistry.Setup(t => t.GetAllTools()).Returns(new List<ITool>());

            var orchestrator = new ToolOrchestratorService(mockToolRegistry.Object, mockLogger.Object);

            // Act
            var result = await orchestrator.ExecuteToolAsync("NonExistentTool", "Test query", new Dictionary<string, string>());

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Tool not found", result.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteBestToolAsync_WhenBestToolExists_ReturnsToolResult()
        {
            // Arrange
            var mockToolRegistry = new Mock<IToolRegistry>();
            var mockLogger = new Mock<ILogger<ToolOrchestratorService>>();

            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns("TestTool");
            mockTool.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new ToolResult { Success = true, Result = "Test result" });

            mockToolRegistry.Setup(t => t.FindBestToolForQueryAsync(It.IsAny<string>()))
                .ReturnsAsync(mockTool.Object);

            var orchestrator = new ToolOrchestratorService(mockToolRegistry.Object, mockLogger.Object);

            // Act
            var result = await orchestrator.ExecuteBestToolAsync("Test query", new Dictionary<string, string>());

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Test result", result.Result);
        }

        [Fact]
        public async Task ExecuteBestToolAsync_WhenBestToolIsChatTool_ReturnsError()
        {
            // Arrange
            var mockToolRegistry = new Mock<IToolRegistry>();
            var mockLogger = new Mock<ILogger<ToolOrchestratorService>>();

            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns("Chat");

            mockToolRegistry.Setup(t => t.FindBestToolForQueryAsync(It.IsAny<string>()))
                .ReturnsAsync(mockTool.Object);

            var orchestrator = new ToolOrchestratorService(mockToolRegistry.Object, mockLogger.Object);

            // Act
            var result = await orchestrator.ExecuteBestToolAsync("Test query", new Dictionary<string, string>());

            // Assert
            Assert.False(result.Success);
            Assert.Contains("prevent recursive loops", result.ErrorMessage);
        }

        [Fact]
        public void GetAllTools_ExcludesChatTools()
        {
            // Arrange
            var mockToolRegistry = new Mock<IToolRegistry>();
            var mockLogger = new Mock<ILogger<ToolOrchestratorService>>();

            var mockTools = new List<ITool>
            {
                CreateMockTool("Search", "Search tool"),
                CreateMockTool("Calculator", "Calculator tool"),
                CreateMockTool("Weather", "Weather tool"),
                CreateMockTool("Chat", "Chat tool"),
                CreateMockTool("StreamingChat", "Streaming chat tool")
            };

            mockToolRegistry.Setup(t => t.GetAllTools()).Returns(mockTools);

            var orchestrator = new ToolOrchestratorService(mockToolRegistry.Object, mockLogger.Object);

            // Act
            var tools = orchestrator.GetAllTools();
            var toolList = tools.ToList();

            // Assert
            Assert.Equal(3, toolList.Count);
            Assert.Contains(toolList, t => t.Name == "Search");
            Assert.Contains(toolList, t => t.Name == "Calculator");
            Assert.Contains(toolList, t => t.Name == "Weather");
            Assert.DoesNotContain(toolList, t => t.Name == "Chat");
            Assert.DoesNotContain(toolList, t => t.Name == "StreamingChat");
        }

        private ITool CreateMockTool(string name, string description)
        {
            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns(name);
            mockTool.Setup(t => t.Description).Returns(description);
            return mockTool.Object;
        }
    }
}