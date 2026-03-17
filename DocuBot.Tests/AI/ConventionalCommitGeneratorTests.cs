using DocuBot.AI.Services;
using Xunit;

namespace DocuBot.Tests.AI
{
    public class ConventionalCommitGeneratorTests
    {
        [Theory]
        [InlineData("feat(core): add new feature", "diff", "feat(core): add new feature")]
        [InlineData("fix: correct bug", "diff", "fix: correct bug")]
        [InlineData("docs(readme): update docs.", "diff", "docs(readme): update docs")]
        [InlineData("invalid commit message", "diff", "Unable to determine appropriate commit type.")]
        [InlineData("feat(core): added new feature", "diff", "Unable to determine appropriate commit type.")]
        public void Generate_ReturnsExpected(string aiSuggestion, string diff, string expected)
        {
            var generator = new ConventionalCommitGenerator();
            var result = generator.Generate(aiSuggestion, diff);
            Assert.Equal(expected, result);
        }
    }
}
