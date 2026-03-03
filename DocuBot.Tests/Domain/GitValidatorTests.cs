using DocuBot.Domain.Services;
using Xunit;

namespace DocuBot.Tests.Domain
{
    public class GitValidatorTests
    {
        [Theory]
        [InlineData("feature/new-feature", true)]
        [InlineData("bugfix/issue-123", true)]
        [InlineData("hotfix/urgent-fix", true)]
        [InlineData("dev/new-feature", false)]
        [InlineData("main", false)]
        public void ValidateBranchName_WorksAsExpected(string branch, bool expected)
        {
            var validator = new GitValidator();
            Assert.Equal(expected, validator.ValidateBranchName(branch));
        }

        [Theory]
        [InlineData("feat(core): add new feature", true)]
        [InlineData("fix: correct typo", true)]
        [InlineData("docs(readme): update documentation", true)]
        [InlineData("invalid commit message", false)]
        [InlineData("feature: missing scope", false)]
        public void ValidateCommitMessage_WorksAsExpected(string message, bool expected)
        {
            var validator = new GitValidator();
            Assert.Equal(expected, validator.ValidateCommitMessage(message));
        }
    }
}
