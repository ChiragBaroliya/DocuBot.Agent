using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DocuBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocuBot.Infrastructure.Services
{
    public class WebApiAiModelService : IAiModelService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public WebApiAiModelService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = (configuration["WebApi:BaseUrl"] ?? "http://localhost:5165").TrimEnd('/');
        }

        public async Task<string> GenerateCommitMessageAsync(string diff)
        {
            var payload = new { Diff = diff };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/commit/suggest", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> ValidateCommitMessageAsync(string commitMessage, string diff)
        {
            var payload = new 
            { 
                CommitMessage = commitMessage, 
                Diff = diff 
            };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/commit/validate", payload);
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(responseText);
        }

        public async Task<string> GenerateCodeReviewHtmlReportAsync(string diff)
        {
            var payload = new { Diff = diff };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/commit/review-report", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public Task<string> GetResponseAsync(string model, string input)
        {
            throw new NotImplementedException("GetResponseAsync is not supported in Web API mode.");
        }

        public Task<string> GeneratePRDescriptionAsync(string diff)
        {
            throw new NotImplementedException("GeneratePRDescriptionAsync is not supported in Web API mode.");
        }

        public Task<string> GenerateDocumentationAsync(string codeOrComments)
        {
            throw new NotImplementedException("GenerateDocumentationAsync is not supported in Web API mode.");
        }

        public Task<string> GenerateCodeReviewAsync(string diff)
        {
            throw new NotImplementedException("GenerateCodeReviewAsync is not supported in Web API mode.");
        }
    }
}
