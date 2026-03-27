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
            _apiKey = apiKey;
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

        public async Task<bool> ValidateCommitMessageAsync(string commitMessage, string diff)
        {
            var prompt = $"Analyze the provided git diff and the given commit message.\n" +
                         $"Does the commit message accurately describe the changes in the git diff?\n" +
                         $"Respond with ONLY 'true' if accurate, or 'false' if incorrect.\n\n" +
                         $"Commit Message:\n{commitMessage}\n\n" +
                         $"Git Diff:\n{diff}";
                         
            var response = await SendPromptAsync(prompt);
            return response.Trim().Equals("true", System.StringComparison.OrdinalIgnoreCase);
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

        public Task<string> GenerateCodeReviewAsync(string diff)
        {
            var prompt = "You are an expert security code reviewer focusing on OWASP Top 10 security risks.\n" +
                         "Review the provided git diff for staged files and provide suggestions ONLY for HIGH and CRITICAL severity issues related to OWASP Top 10 (e.g., SQL injection, Sensitive Data Exposure, etc.).\n" +
                         "If there are no HIGH or CRITICAL issues, respond with 'Status: PASS - No high or critical issues found.'\n" +
                         "Otherwise, respond with 'Status: REVIEW_REQUIRED' followed by a detailed markdown list of the violations including the specific OWASP category.\n" +
                         "IMPORTANT: Your response MUST contain either 'Status: PASS' or 'Status: REVIEW_REQUIRED' prominently.\n" +
                         $"Format the output as a Markdown report.\n\nGit Diff:\n{diff}";
            return SendPromptAsync(prompt);
        }
    }
}
