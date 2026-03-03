using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocuBot.Agent.Services
{
    public interface IMcpService
    {
        Task<IList<string>> FetchRepositoryFilesAsync();
        Task<string> FetchFileContentAsync(string filePath);
        Task CommitMarkdownDocumentationAsync(string filePath, string markdownContent);
    }
}