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
            var expectedPrompt = "Respond with ONLY the git commit message. No explanation. No quotes.\n\nGenerating commit message with prompt:\n\ndiff --git a/src/App.js b/src/App.js\nindex 7cfc5f9..cdf3d0e 100644\n--- a/src/App.js\n+++ b/src/App.js\n@@ -5,12 +5,21 @@\nconstructor(props) {\nthis.state = { searchQuery: '' };\n}\n\nhandleSearch = (query) => {\nthis.setState({ searchQuery: query });\n}";
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
            var result = await service.GenerateCommitMessageAsync(expectedPrompt);

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
