using System.Threading.Tasks;

namespace DocuBot.AI.Interfaces
{
    public interface IOpenAIService
    {
        Task<string> GenerateCommitMessageAsync(string diff);
        Task<string> GeneratePRDescriptionAsync(string diff);
        Task<string> GenerateDocumentationAsync(string codeOrComments);
    }
}
