using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using System;


namespace DocuBot.Infrastructure.Services
{
    public class OllamaService : IAiModelService
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

        public async Task<bool> ValidateCommitMessageAsync(string commitMessage, string diff)
        {
            var prompt = $"Analyze the provided git diff and the given commit message.\n" +
                         $"Does the commit message accurately describe the changes in the git diff?\n" +
                         $"Respond with ONLY 'true' if accurate, or 'false' if incorrect.\n\n" +
                         $"Commit Message:\n{commitMessage}\n\n" +
                         $"Git Diff:\n{diff}";
                         
            var response = await GenerateOllamaResponseAsync(prompt);
            return response.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
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

        public async Task<string> GenerateCodeReviewAsync(string diff)
        {
            var prompt = "You are an expert security code reviewer focusing on OWASP Top 10 security risks.\n" +
                         "Review the provided git diff for staged files and provide suggestions ONLY for HIGH and CRITICAL severity issues related to OWASP Top 10 (e.g., SQL injection, Sensitive Data Exposure, etc.).\n" +
                         "If there are no HIGH or CRITICAL issues, respond with 'Status: PASS - No high or critical issues found.'\n" +
                         "Otherwise, respond with 'Status: REVIEW_REQUIRED' followed by a detailed markdown list of the violations including the specific OWASP category.\n" +
                         "IMPORTANT: Your response MUST contain either 'Status: PASS' or 'Status: REVIEW_REQUIRED' prominently.\n" +
                         $"Format the output as a Markdown report.\n\nGit Diff:\n{diff}";
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
            client.Timeout = TimeSpan.FromMinutes(5); // Increase timeout to 5 minutes
            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            try
            {
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
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OllamaService request timed out after {Timeout} minutes.", client.Timeout.TotalMinutes);
                return "ERROR: AI request timed out. Please try again or check Ollama server availability.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OllamaService request failed.");
                return $"ERROR: AI request failed: {ex.Message}";
            }
        }
    }
}
