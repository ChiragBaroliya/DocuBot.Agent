using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace DocuBot.Agent.Services
{
    public class McpService : IMcpService
    {
        private readonly HttpClient _httpClient;

        public McpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Fetches the list of repository files from MCP
        public async Task<IList<string>> FetchRepositoryFilesAsync()
        {
            // Replace with your MCP API endpoint
            var response = await _httpClient.GetFromJsonAsync<List<string>>("https://your-mcp-server/api/repository/files");
            return response ?? new List<string>();
        }

        // Fetches the content of a file from MCP
        public async Task<string> FetchFileContentAsync(string filePath)
        {
            // Replace with your MCP API endpoint
            var response = await _httpClient.GetAsync($"https://your-mcp-server/api/repository/file?path={filePath}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Commits markdown documentation to the repository via MCP
        public async Task CommitMarkdownDocumentationAsync(string filePath, string markdownContent)
        {
            // Replace with your MCP API endpoint and request model
            var payload = new
            {
                Path = filePath,
                Content = markdownContent
            };
            var response = await _httpClient.PostAsJsonAsync("https://your-mcp-server/api/repository/commit", payload);
            response.EnsureSuccessStatusCode();
        }
    }
}