using DocuBot.AI.Services;
using DocuBot.AI.Options;
using DocuBot.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace DocuBot.Tests.AI
{
    public class OpenAIServiceTests
    {
        [Fact]
        public async Task GenerateCommitMessageAsync_ReturnsStrictMessage()
        {
            var httpClient = new System.Net.Http.HttpClient(new MockHttpMessageHandler());
            var options = Options.Create(new OpenAIOptions { Endpoint = "https://api.openai.com/v1/chat/completions" });
            var logger = new Mock<ILogger<OpenAIService>>().Object;
            var generator = new ConventionalCommitGenerator();
            var service = new OpenAIService(httpClient, options, logger, generator);

            var result = await service.GenerateCommitMessageAsync("diff");
            Assert.NotNull(result);
        }

        private class MockHttpMessageHandler : System.Net.Http.HttpMessageHandler
        {
            protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                var response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new System.Net.Http.StringContent("{\"choices\":[{\"message\":{\"content\":\"Code Committed   \"}}]}")
                };
                return Task.FromResult(response);
            }
        }

        private class ErrorMockHttpMessageHandler : System.Net.Http.HttpMessageHandler
        {
            protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                var response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new System.Net.Http.StringContent("{\"error\":\"bad request\"}")
                };
                return Task.FromResult(response);
            }
        }

        private class SuccessMockHttpMessageHandler : System.Net.Http.HttpMessageHandler
        {
            protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                var response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new System.Net.Http.StringContent("{\"choices\":[{\"message\":{\"content\":\"commit message   \"}}]}")
                };
                return Task.FromResult(response);
            }
        }
    }
}
