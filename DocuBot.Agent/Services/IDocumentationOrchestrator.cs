using System.Threading.Tasks;

namespace DocuBot.Agent.Services
{
    public interface IDocumentationOrchestrator
    {
        Task GenerateAndCommitDocumentationAsync();
    }
}