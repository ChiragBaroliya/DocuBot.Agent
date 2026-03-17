using System.Collections.Generic;
using System.Threading.Tasks;
using DocuBot.AI.Interfaces;
using DocuBot.Infrastructure.Services;
using DocuBot.Agent.Services;

namespace DocuBot.Agent.Services
{
    public class DocumentationOrchestrator : IDocumentationOrchestrator
    {
        private readonly IMcpService _mcpService;
        private readonly IAiModelService _aiModelService;

        public DocumentationOrchestrator(IMcpService mcpService, IAiModelService aiModelService)
        {
            _mcpService = mcpService;
            _aiModelService = aiModelService;
        }

        public async Task GenerateAndCommitDocumentationAsync()
        {
            var files = await _mcpService.FetchRepositoryFilesAsync();
            foreach (var file in files)
            {
                // Fetch file content from MCP
                var codeContent = await _mcpService.FetchFileContentAsync(file);
                var markdown = await _aiModelService.GenerateDocumentationAsync(codeContent);
                var markdownFilePath = file + ".md";
                await _mcpService.CommitMarkdownDocumentationAsync(markdownFilePath, markdown);
            }
        }
    }
}