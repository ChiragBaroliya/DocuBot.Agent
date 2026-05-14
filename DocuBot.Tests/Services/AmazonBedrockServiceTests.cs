using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using DocuBot.Infrastructure.Services;
using Moq;
using Xunit;

namespace DocuBot.Tests.Services
{
    public class AmazonBedrockServiceTests
    {
        private readonly Mock<IAmazonBedrockRuntime> _mockBedrockClient;
        private readonly AmazonBedrockService _service;

        public AmazonBedrockServiceTests()
        {
            _mockBedrockClient = new Mock<IAmazonBedrockRuntime>();
            _service = new AmazonBedrockService(_mockBedrockClient.Object);
        }

        [Fact]
        public async Task GetResponseAsync_ShouldReturnText_WhenResponseIsSuccessful()
        {
            // Arrange
            var modelId = "meta.llama3-3-70b-instruct-v1:0";
            var input = "Hello, AI!";
            var expectedOutput = "Hello! How can I help you today?";
            
            var responseJson = JsonSerializer.Serialize(new { generation = expectedOutput });
            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responseJson));
            
            var response = new InvokeModelResponse
            {
                Body = responseStream,
                HttpStatusCode = System.Net.HttpStatusCode.OK
            };

            _mockBedrockClient
                .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetResponseAsync(modelId, input);

            // Assert
            Assert.Equal(expectedOutput, result);
            _mockBedrockClient.Verify(c => c.InvokeModelAsync(
                It.Is<InvokeModelRequest>(r => r.ModelId == modelId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetResponseAsync_ShouldReturnErrorMessage_WhenExceptionOccurs()
        {
            // Arrange
            var exceptionMessage = "AWS connection failed";
            _mockBedrockClient
                .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _service.GetResponseAsync("test-model", "test-input");

            // Assert
            Assert.Contains("[AmazonBedrockService Error]", result);
            Assert.Contains(exceptionMessage, result);
        }

        [Fact]
        public async Task ValidateCommitMessageAsync_ShouldReturnTrue_WhenResponseContainsTrue()
        {
            // Arrange
            var responseJson = JsonSerializer.Serialize(new { generation = "true" });
            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responseJson));
            
            var response = new InvokeModelResponse
            {
                Body = responseStream,
                HttpStatusCode = System.Net.HttpStatusCode.OK
            };

            _mockBedrockClient
                .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.ValidateCommitMessageAsync("feat: add tests", "diff...");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateCommitMessageAsync_ShouldReturnFalse_WhenResponseDoesNotContainTrue()
        {
            // Arrange
            var responseJson = JsonSerializer.Serialize(new { generation = "false" });
            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responseJson));
            
            var response = new InvokeModelResponse
            {
                Body = responseStream,
                HttpStatusCode = System.Net.HttpStatusCode.OK
            };

            _mockBedrockClient
                .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.ValidateCommitMessageAsync("feat: add tests", "diff...");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GenerateCommitMessageAsync_ShouldReturnFormattedCommitMessage()
        {
            // Arrange
            var diff = "some git diff";
            var expectedMessage = "feat: add unit tests\n\nDetailed description of changes.";
            
            var responseJson = JsonSerializer.Serialize(new { generation = expectedMessage });
            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responseJson));
            
            var response = new InvokeModelResponse
            {
                Body = responseStream,
                HttpStatusCode = System.Net.HttpStatusCode.OK
            };

            _mockBedrockClient
                .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.GenerateCommitMessageAsync(diff);

            // Assert
            Assert.Equal(expectedMessage, result);
        }

        [Fact]
        public async Task ExtractTextFromResponse_ShouldHandleInvalidJson()
        {
             // Arrange
            var invalidJson = "invalid json";
            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
            
            var response = new InvokeModelResponse
            {
                Body = responseStream,
                HttpStatusCode = System.Net.HttpStatusCode.OK
            };

            _mockBedrockClient
                .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetResponseAsync("test-model", "test-input");

            // Assert
            Assert.Contains("[AI Response Parsing Error]", result);
        }
    }
}
