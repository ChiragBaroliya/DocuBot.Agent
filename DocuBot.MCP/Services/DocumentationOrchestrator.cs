using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DocuBot.MCP.Services
{
    public class DocumentationOrchestrator
    {
        private readonly IMcpService _mcpService;
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<DocumentationOrchestrator> _logger;

        public DocumentationOrchestrator(IMcpService mcpService, IOpenAIService openAIService, ILogger<DocumentationOrchestrator> logger)
        {
            _mcpService = mcpService;
            _openAIService = openAIService;
            _logger = logger;
        }

        public async Task GenerateCommitDocumentationAsync()
        {
            try
            {
                _logger.LogInformation("Starting commit documentation generation.");
                var diff = await _mcpService.GetLatestCommitDiffAsync();
                var files = await _mcpService.GetCommitFilesAsync();
                var fileContents = new Dictionary<string, string>();
                foreach (var file in files)
                {
                    fileContents[file] = await _mcpService.GetFileContentAsync(file);
                }
                var aiInput = new
                {
                    diff,
                    files,
                    fileContents
                };
                var markdown = await _openAIService.GenerateDocumentationAsync(aiInput);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var docPath = $"docs/commit-{timestamp}.md";
                var writeResult = await _mcpService.WriteFileAsync(docPath, markdown);
                if (writeResult)
                    _logger.LogInformation($"Documentation saved to {docPath}");
                else
                    _logger.LogError($"Failed to save documentation to {docPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating commit documentation.");
            }
        }
    }
}
