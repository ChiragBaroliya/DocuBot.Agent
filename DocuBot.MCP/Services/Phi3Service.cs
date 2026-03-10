using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DocuBot.MCP.Services
{
    public class AiModelService : IOpenAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AiModelService> _logger;

        public AiModelService(IHttpClientFactory httpClientFactory, ILogger<AiModelService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> GenerateDocumentationAsync(object input)
        {
            var prompt = input is string s ? s : JsonSerializer.Serialize(input);
            var requestBody = new
            {
                model = "llama3:8b",
                prompt = prompt,
                stream = false
            };

            var client = _httpClientFactory.CreateClient();
            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://192.168.0.220:11434/api/generate", requestContent);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("response", out var responseProp))
            {
                return responseProp.GetString() ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
