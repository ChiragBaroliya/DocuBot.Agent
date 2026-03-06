using System.Threading.Tasks;
using System.Collections.Generic;

namespace DocuBot.MCP.Services
{
    public interface IMcpService
    {
        Task<string> GetLatestCommitDiffAsync();
        Task<IReadOnlyList<string>> GetCommitFilesAsync();
        Task<string> GetFileContentAsync(string filePath);
        Task<bool> WriteFileAsync(string filePath, string content);
    }
}
