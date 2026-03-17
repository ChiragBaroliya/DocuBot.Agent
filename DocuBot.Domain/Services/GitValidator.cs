using System.Text.RegularExpressions;
using DocuBot.Domain.Interfaces;

namespace DocuBot.Domain.Services
{
    public class GitValidator : IGitValidator
    {
        public bool ValidateBranchName(string branchName)
        {
            return branchName.StartsWith("feature/") ||
                   branchName.StartsWith("bugfix/") ||
                   branchName.StartsWith("hotfix/");
        }

        public bool ValidateCommitMessage(string commitMessage)
        {
            // Conventional Commit format: type(scope): description
            return Regex.IsMatch(commitMessage, @"^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .+");
        }
    }
}
