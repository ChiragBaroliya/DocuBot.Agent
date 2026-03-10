using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using DocuBot.MCP.Services;
using Xunit;
using DocuBot.Infrastructure.Services;

namespace DocuBot.Tests.Services
{
    public class AiModelServiceTests
    {
        [Fact]
        public async Task GenerateDocumentationAsync_ReturnsResponse()
        {
            // Arrange
            var expectedPrompt = "Test prompt";
            var expectedResponse = "Commit message";
            var jsonResponse = $"{{\"response\":\"{expectedResponse}\"}}";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var loggerMock = new Mock<ILogger<OllamaService>>();
            var service = new OllamaService(httpClientFactoryMock.Object, loggerMock.Object);

            // Act
            var result = await service.GenerateDocumentationAsync(expectedPrompt);

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task GenerateDocumentationAsync_HandlesEmptyResponse()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var loggerMock = new Mock<ILogger<OllamaService>>();
            var service = new OllamaService(httpClientFactoryMock.Object, loggerMock.Object);

            // Act
            var result = await service.GenerateDocumentationAsync("Test");

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
