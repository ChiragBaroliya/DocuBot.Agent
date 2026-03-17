using DocuBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocuBot.Tests.Infrastructure
{
    public class GitExecutorTests
    {
        [Fact]
        public void ValidateBranch_ValidBranch_ReturnsTrue()
        {
            var logger = new Mock<ILogger<GitExecutor>>();
            var executor = new GitExecutor(logger.Object);
            Assert.True(executor.ValidateBranch("feature/test"));
        }

        [Fact]
        public void ValidateBranch_InvalidBranch_ReturnsFalse()
        {
            var logger = new Mock<ILogger<GitExecutor>>();
            var executor = new GitExecutor(logger.Object);
            Assert.False(executor.ValidateBranch("main"));
        }

        [Fact]
        public void ValidateCommit_ValidCommit_ReturnsTrue()
        {
            var logger = new Mock<ILogger<GitExecutor>>();
            var executor = new GitExecutor(logger.Object);
            Assert.True(executor.ValidateCommit("feat(core): add feature"));
        }

        [Fact]
        public void ValidateCommit_InvalidCommit_ReturnsFalse()
        {
            var logger = new Mock<ILogger<GitExecutor>>();
            var executor = new GitExecutor(logger.Object);
            Assert.False(executor.ValidateCommit("invalid commit"));
        }
    }
}
