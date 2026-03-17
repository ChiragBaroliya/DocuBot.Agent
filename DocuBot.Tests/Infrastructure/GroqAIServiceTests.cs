using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using DocuBot.Infrastructure.Services;

namespace DocuBot.Tests.Infrastructure
{
    public class GroqAIServiceTests
    {
        [Fact]
        public async Task GenerateCommitMessageAsync_ReturnsCommitMessage()
        {
            // Arrange
            var diff = "diff --git a/file.txt b/file.txt\n...";
            var expectedCommitMessage = "Add new feature";
            var jsonResponse = @"{
  ""choices"": [
    {
      ""message"": {
        ""content"": ""Add new feature""
      }
    }
  ]
}";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var service = new GroqAIService(httpClient, "fakeKey");

            // Act
            var result = await service.GenerateCommitMessageAsync(diff);

            // Assert
            Assert.Equal(expectedCommitMessage, result);
        }

        [Fact]
        public async Task GenerateCommitMessageAsync_HandlesParsingError()
        {
            // Arrange
            var diff = "diff --git a/file.txt b/file.txt\n...";
            var invalidJsonResponse = "not a json";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(invalidJsonResponse)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var service = new GroqAIService(httpClient, "fakeKey");

            // Act
            var result = await service.GenerateCommitMessageAsync(diff);

            // Assert
            Assert.Contains("[AI Response Parsing Error]", result);
            Assert.Contains("not a json", result);
        }
    }
}
