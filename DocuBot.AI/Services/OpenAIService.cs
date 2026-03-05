using DocuBot.AI.Interfaces;
using DocuBot.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocuBot.AI.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIService> _logger;
        private readonly OpenAIOptions _options;
        private readonly IConventionalCommitGenerator _commitGenerator;

        public OpenAIService(
            HttpClient httpClient,
            IOptions<OpenAIOptions> options,
            ILogger<OpenAIService> logger,
            IConventionalCommitGenerator commitGenerator)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
            _commitGenerator = commitGenerator;
        }

        public async Task<string> GenerateCommitMessageAsync(string diff)
        {
            var aiSuggestion = await SendChatCompletionAsync($"Generate a Conventional Commit message for this diff:\n{diff}");
            var strictMessage = _commitGenerator.Generate(aiSuggestion, diff);
            return strictMessage;
        }

        public async Task<string> GeneratePRDescriptionAsync(string diff)
        {
            return await SendChatCompletionAsync($"Summarize this diff for a PR description:\n{diff}");
        }

        public async Task<string> GenerateDocumentationAsync(string codeOrComments)
        {
            return await SendChatCompletionAsync($"Generate markdown documentation for the following code or comments:\n{codeOrComments}");
        }

        private async Task<string> SendChatCompletionAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogError("OpenAI API key is missing.");
                throw new InvalidOperationException("OpenAI API key is missing.");
            }

            var endpoint = string.IsNullOrWhiteSpace(_options.Endpoint)
                ? "https://api.openai.com"
                : _options.Endpoint.TrimEnd('/');

            var url = $"{endpoint}/v1/chat/completions";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new {
                        role = "system",
                        content = "You are an assistant that writes concise, conventional commit messages. Only describe the actual code changes in the diff. Use the correct type (feat, fix, etc.), scope, and a short summary. Do not add extra context or unrelated information."
                    },
                    new { role = "user", content = prompt }
                },
                max_tokens = 50,
                temperature = 0.2
            };

            var requestJson = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API error: {Status} {Content}", response.StatusCode, responseContent);
                throw new Exception($"OpenAI API error: {response.StatusCode} {responseContent}");
            }

            using var doc = JsonDocument.Parse(responseContent);

            var message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return message?.Trim() ?? "No commit message generated.";
        }
    }
}
