namespace DocuBot.Application.Interfaces
{
    using System.Threading.Tasks;
    public interface IAiModelService
    {
        Task<string> GetResponseAsync(string model, string input);
        Task<string> GenerateCommitMessageAsync(string diff);
        Task<bool> ValidateCommitMessageAsync(string commitMessage, string diff);
        Task<string> GeneratePRDescriptionAsync(string diff);
        Task<string> GenerateDocumentationAsync(string codeOrComments);
        Task<string> GenerateCodeReviewAsync(string diff);
    }
}
