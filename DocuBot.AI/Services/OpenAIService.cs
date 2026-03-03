using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DocuBot.AI.Interfaces;
using DocuBot.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            var aiSuggestion = await SendPromptAsync($"Generate a Conventional Commit message for this diff:\n{diff}");
            var strictMessage = _commitGenerator.Generate(aiSuggestion, diff);
            return strictMessage;
        }

        public async Task<string> GeneratePRDescriptionAsync(string diff)
        {
            return await SendPromptAsync($"Summarize this diff for a PR description:\n{diff}");
        }

        private async Task<string> SendPromptAsync(string prompt)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    var request = new { prompt };
                    var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, request);
                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
                    return result?.Choices?.FirstOrDefault()?.Text ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OpenAI request failed. Attempt {RetryCount}", retryCount + 1);
                    if (++retryCount >= 3) throw;
                    await Task.Delay(1000 * retryCount);
                }
            }
        }
    }

    public class OpenAIResponse
    {
        public List<OpenAIChoice> Choices { get; set; } = new();
    }
    public class OpenAIChoice
    {
        public string Text { get; set; } = string.Empty;
    }
}
