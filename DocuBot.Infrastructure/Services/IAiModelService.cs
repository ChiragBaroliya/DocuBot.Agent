namespace DocuBot.Infrastructure.Services
{
    using System.Threading.Tasks;
    public interface IAiModelService
    {
        Task<string> GenerateCommitMessageAsync(string diff);
        Task<string> GeneratePRDescriptionAsync(string diff);
        Task<string> GenerateDocumentationAsync(string codeOrComments);
    }
}
