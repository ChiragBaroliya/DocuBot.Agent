using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocuBot.Infrastructure.Services
{
    public class GoogleGeminiAPIService : IAiModelService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent";

        public GoogleGeminiAPIService(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = "AIzaSyBKQ2GVOpKyfD2pwBpUea1UDoWNwSEfX_g";
        }

        private async Task<string> SendPromptAsync(string prompt)
        {
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}?key={_apiKey}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            var text = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            return text ?? string.Empty;
        }

        public Task<string> GenerateCommitMessageAsync(string diff)
        {
            var prompt = $"Generate a concise commit message for the following diff:\n{diff}";
            return SendPromptAsync(prompt);
        }

        public Task<string> GeneratePRDescriptionAsync(string diff)
        {
            var prompt = $"Generate a pull request description for the following diff:\n{diff}";
            return SendPromptAsync(prompt);
        }

        public Task<string> GenerateDocumentationAsync(string codeOrComments)
        {
            var prompt = $"Generate documentation for the following code or comments:\n{codeOrComments}";
            return SendPromptAsync(prompt);
        }
    }
}
