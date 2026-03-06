using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;

namespace DocuBot.MCP.Services
{
    public class McpService : IMcpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<McpService> _logger;
        private readonly IConfiguration _config;
        private readonly string _gitServerUrl;
        private readonly string _filesystemServerUrl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public McpService(IHttpClientFactory httpClientFactory, ILogger<McpService> logger, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = config;
            _gitServerUrl = _config["McpServers:GitServer"];
            _filesystemServerUrl = _config["McpServers:FilesystemServer"];
            _retryPolicy = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => System.TimeSpan.FromSeconds(retryAttempt),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, $"Retry {retryCount} after {timeSpan.TotalSeconds}s");
                    });
        }

        public async Task<string> GetLatestCommitDiffAsync()
        {
            var payload = JsonSerializer.Serialize(new { tool = "get_git_diff", arguments = new { } });
            return await CallMcpAsync(_gitServerUrl, payload);
        }

        public async Task<IReadOnlyList<string>> GetCommitFilesAsync()
        {
            var payload = JsonSerializer.Serialize(new { tool = "get_commit_files", arguments = new { } });
            var response = await CallMcpAsync(_gitServerUrl, payload);
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(response);
        }

        public async Task<string> GetFileContentAsync(string filePath)
        {
            var payload = JsonSerializer.Serialize(new { tool = "read_file", arguments = new { path = filePath } });
            return await CallMcpAsync(_filesystemServerUrl, payload);
        }

        public async Task<bool> WriteFileAsync(string filePath, string content)
        {
            var payload = JsonSerializer.Serialize(new { tool = "write_file", arguments = new { path = filePath, content = content } });
            var response = await CallMcpAsync(_filesystemServerUrl, payload);
            return response.Contains("success");
        }

        private async Task<string> CallMcpAsync(string url, string payload)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"MCP call to {url} succeeded.");
                return content;
            });
        }
    }
}
