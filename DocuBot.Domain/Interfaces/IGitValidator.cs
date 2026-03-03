using System.Text.RegularExpressions;

namespace DocuBot.Domain.Interfaces
{
    public interface IGitValidator
    {
        bool ValidateBranchName(string branchName);
        bool ValidateCommitMessage(string commitMessage);
    }
}
