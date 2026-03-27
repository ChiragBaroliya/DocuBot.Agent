using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocuBot.Infrastructure.Services
{
    public class GroqAIService : IAiModelService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

        public GroqAIService(HttpClient httpClient, string apiKey)
        {

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Groq API key must be provided and cannot be empty.", nameof(apiKey));
            
            _httpClient = httpClient;
            _apiKey = apiKey;
            _model = Environment.GetEnvironmentVariable("GROQAI_MODEL") ?? "llama-3.3-70b-versatile";
        }

        public async Task<string> GetResponseAsync(string model, string input)
        {
            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = input }
                }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, GroqApiUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return $"[GroqAIService Error] Unauthorized (401): Check your API key. Response: {errorContent}";
                    }
                    return $"[GroqAIService Error] HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {errorContent}";
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                return $"[GroqAIService Error] HTTP request failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"[GroqAIService Error] Unexpected error: {ex.Message}";
            }
        }
        public async Task<string> GenerateCommitMessageAsync(string diff)
        {
            string guidance =
                "Use Conventional Commits format:\n" +
                "feat: A new feature\n" +
                "fix: A bug fix\n" +
                "docs: Documentation only\n" +
                "style: Formatting only\n" +
                "refactor: Code restructuring\n" +
                "perf: Performance improvement\n" +
                "test: Adding/updating tests\n" +
                "chore: Maintenance tasks\n\n" +
                "IMPORTANT:\n" +
                "- Return ONLY ONE LINE commit message\n" +
                "- Do NOT include explanation\n" +
                "- Format must be: type: short description\n\n";

            string prompt = $"{guidance}Git diff:\n{diff}";

            string model = _model;

            var responseJson = await GetResponseAsync(model, prompt);

            return ExtractTextFromResponse(responseJson);
        }

        public async Task<bool> ValidateCommitMessageAsync(string commitMessage, string diff)
        {
            string guidance =
                "You are a strict git commit reviewer. Analyze the provided git diff and the given commit message.\n" +
                "Does the commit message accurately describe the changes in the git diff?\n" +
                "Return EXACTLY 'true' if the message is accurate and appropriate for the changes.\n" +
                "Return EXACTLY 'false' if the message is incorrect, misleading, or completely unrelated.\n" +
                "IMPORTANT: Return ONLY the word 'true' or 'false', nothing else.";

            string prompt = $"{guidance}\n\nCommit Message:\n{commitMessage}\n\nGit Diff:\n{diff}";
            string model = _model;

            var responseJson = await GetResponseAsync(model, prompt);
            var responseText = ExtractTextFromResponse(responseJson).Trim().ToLower();

            return responseText == "true";
        }



        public async Task<string> GeneratePRDescriptionAsync(string diff)
        {
            string prompt = $"Write a detailed pull request description for the following code changes:\n{diff}";
            string model = _model;
            var responseJson = await GetResponseAsync(model, prompt);
            return ExtractTextFromResponse(responseJson);
        }

        public async Task<string> GenerateDocumentationAsync(string codeOrComments)
        {
            string prompt = $"Generate documentation comments for the following code or comments:\n{codeOrComments}";
            string model = _model;
            var responseJson = await GetResponseAsync(model, prompt);
            return ExtractTextFromResponse(responseJson);
        }

        public async Task<string> GenerateCodeReviewAsync(string diff)
        {
            string guidance = 
                "You are an expert security code reviewer focusing on OWASP Top 10 security risks.\n" +
                "Review the provided git diff for staged files and provide suggestions ONLY for HIGH and CRITICAL severity issues related to OWASP Top 10 (e.g., SQL injection, Sensitive Data Exposure, etc.).\n" +
                "If there are no HIGH or CRITICAL issues, respond with 'Status: PASS - No high or critical issues found.'\n" +
                "Otherwise, respond with 'Status: REVIEW_REQUIRED' followed by a detailed markdown list of the violations including the specific OWASP category.\n" +
                "IMPORTANT: Your response MUST contain either 'Status: PASS' or 'Status: REVIEW_REQUIRED' prominently.\n" +
                "Format the output as a Markdown report.\n";

            string prompt = $"{guidance}\n\nGit Diff:\n{diff}";
            string model = _model; 

            var responseJson = await GetResponseAsync(model, prompt);
            return ExtractTextFromResponse(responseJson);
        }

        private string ExtractTextFromResponse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                // Try to extract the first text value from any content array/object in the output array
                if (doc.RootElement.TryGetProperty("output", out var outputArray) && outputArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var outputItem in outputArray.EnumerateArray())
                    {
                        // content can be an array or object
                        if (outputItem.TryGetProperty("content", out var contentProp))
                        {
                            if (contentProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var contentItem in contentProp.EnumerateArray())
                                {
                                    if (contentItem.TryGetProperty("text", out var textProp))
                                    {
                                        var text = textProp.GetString();
                                        if (!string.IsNullOrWhiteSpace(text))
                                            return text;
                                    }
                                }
                            }
                            else if (contentProp.ValueKind == JsonValueKind.Object)
                            {
                                if (contentProp.TryGetProperty("text", out var textProp))
                                {
                                    var text = textProp.GetString();
                                    if (!string.IsNullOrWhiteSpace(text))
                                        return text;
                                }
                            }
                        }
                    }
                }
                // Fallback to previous Groq format (choices[0].message.content)
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
                {
                    var choice = choices[0];
                    if (choice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                return $"[AI Response Parsing Error]: {ex.Message}\nRaw response: {responseJson}";
            }
            return string.Empty;
        }
    }
}
