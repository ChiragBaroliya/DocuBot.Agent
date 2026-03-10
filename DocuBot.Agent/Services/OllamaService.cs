using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DocuBot.AI.Interfaces;

namespace DocuBot.Agent.Services
{
    public class OllamaService : IOpenAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OllamaService> _logger;

        public OllamaService(IHttpClientFactory httpClientFactory, ILogger<OllamaService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> GenerateCommitMessageAsync(string diff)
        {
            var prompt = $"Respond with ONLY the git commit message. No explanation. No quotes.\n\n{diff}";
            return await GenerateOllamaResponseAsync(prompt);
        }

        public async Task<string> GenerateDocumentationAsync(string codeOrComments)
        {
            var prompt = $"Generate technical markdown documentation for the following code or comments. Focus on code structure, logic, APIs, and deployment details. Avoid general summaries:\n{codeOrComments}";
            return await GenerateOllamaResponseAsync(prompt);
        }

        public async Task<string> GeneratePRDescriptionAsync(string diff)
        {
            var prompt = $"Summarize this diff for a PR description:\n{diff}";
            return await GenerateOllamaResponseAsync(prompt);
        }

        private async Task<string> GenerateOllamaResponseAsync(string prompt)
        {
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
