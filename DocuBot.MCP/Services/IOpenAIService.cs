using System.Threading.Tasks;

namespace DocuBot.MCP.Services
{
    public interface IOpenAIService
    {
        Task<string> GenerateDocumentationAsync(object input);
    }
}
