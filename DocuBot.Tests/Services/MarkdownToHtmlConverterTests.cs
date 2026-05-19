using DocuBot.Infrastructure.Services;
using Xunit;

namespace DocuBot.Tests.Services
{
    public class MarkdownToHtmlConverterTests
    {
        [Fact]
        public void Convert_ShouldConvertHeaders_ToHtmlTags()
        {
            // Arrange
            var markdown = "## Security Issues Found\n### Severity: HIGH";

            // Act
            var html = MarkdownToHtmlConverter.Convert(markdown);

            // Assert
            Assert.Contains("<h2>Security Issues Found</h2>", html);
            Assert.Contains("<h3>Severity: HIGH</h3>", html);
        }

        [Fact]
        public void Convert_ShouldConvertLists_ToUlLiTags()
        {
            // Arrange
            var markdown = "- Use parameterization\n- Restrict credentials";

            // Act
            var html = MarkdownToHtmlConverter.Convert(markdown);

            // Assert
            Assert.Contains("<ul>", html);
            Assert.Contains("<li>Use parameterization</li>", html);
            Assert.Contains("<li>Restrict credentials</li>", html);
            Assert.Contains("</ul>", html);
        }

        [Fact]
        public void Convert_ShouldConvertCodeBlocks_ToPreCodeTags()
        {
            // Arrange
            var markdown = "```csharp\nvar secret = \"123\";\n```";

            // Act
            var html = MarkdownToHtmlConverter.Convert(markdown);

            // Assert
            Assert.Contains("<pre><code>", html);
            Assert.Contains("var secret = &quot;123&quot;;", html);
            Assert.Contains("</code></pre>", html);
        }

        [Fact]
        public void Convert_ShouldConvertBoldAndInlineCode_ToTags()
        {
            // Arrange
            var markdown = "Review is **CRITICAL** for `Program.cs`.";

            // Act
            var html = MarkdownToHtmlConverter.Convert(markdown);

            // Assert
            Assert.Contains("<strong>CRITICAL</strong>", html);
            Assert.Contains("<code>Program.cs</code>", html);
        }

        [Fact]
        public void Convert_ShouldAddCorrectStatusBadge_BasedOnMarkdownContent()
        {
            // Arrange
            var passMarkdown = "Status: PASS - No issues found";
            var failMarkdown = "Status: REVIEW_REQUIRED\nviolations found";

            // Act
            var passHtml = MarkdownToHtmlConverter.Convert(passMarkdown);
            var failHtml = MarkdownToHtmlConverter.Convert(failMarkdown);

            // Assert
            Assert.Contains("badge-success", passHtml);
            Assert.Contains("Status: PASS", passHtml);
            
            Assert.Contains("badge-fail", failHtml);
            Assert.Contains("Status: REVIEW REQUIRED", failHtml);
        }
    }
}
