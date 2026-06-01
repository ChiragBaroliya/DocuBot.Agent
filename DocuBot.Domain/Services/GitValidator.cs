using System.Linq;
using System.Text.RegularExpressions;
using DocuBot.Domain.Interfaces;

namespace DocuBot.Domain.Services
{
    public class GitValidator : IGitValidator
    {
        public bool ValidateBranchName(string branch)
        {
            // Only validate if not master/main/develop
            var ignored = new[] { "master", "main", "develop" };
            if (ignored.Contains(branch.ToLower()))
                return true;
            // Your pattern check here
            return branch.StartsWith("feature/") || branch.StartsWith("bugfix/") || branch.StartsWith("hotfix/");
        }

        public bool ValidateCommitMessage(string commitMessage)
        {
            // Conventional Commit format: type(scope): description
            return Regex.IsMatch(commitMessage, @"^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .+");
        }
    }
}
