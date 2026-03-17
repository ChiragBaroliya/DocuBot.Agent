using System.Linq;
using System.Text.RegularExpressions;
using DocuBot.AI.Interfaces;

namespace DocuBot.AI.Services
{
    public class ConventionalCommitGenerator : IConventionalCommitGenerator
    {
        private static readonly string[] AllowedTypes =
        {
            "feat", "fix", "docs", "style", "refactor", "test", "chore", "perf", "build", "ci"
        };

        public string Generate(string aiSuggestion, string diff)
        {
            // Attempt to parse AI suggestion
            var match = Regex.Match(aiSuggestion,
                @"^(?<type>feat|fix|docs|style|refactor|test|chore|perf|build|ci)(!?)\((?<scope>[^)]*)\)?: (?<summary>.+)$",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return "Unable to determine appropriate commit type.";

            var type = match.Groups["type"].Value.ToLowerInvariant();
            var breaking = match.Groups[2].Value == "!";
            var scope = match.Groups["scope"].Value;
            var summary = match.Groups["summary"].Value.Trim();

            // Validate type
            if (!AllowedTypes.Contains(type))
                return "Unable to determine appropriate commit type.";

            // Validate summary
            if (summary.Length > 72)
                summary = summary.Substring(0, 72);
            if (summary.EndsWith("."))
                summary = summary.TrimEnd('.');

            // Imperative tense check (basic heuristic)
            if (!IsImperative(summary))
                return "Unable to determine appropriate commit type.";

            // Format message
            var typePart = breaking ? $"{type}!" : type;
            var scopePart = string.IsNullOrWhiteSpace(scope) ? "" : $"({scope})";
            var message = $"{typePart}{scopePart}: {summary}";
            return message;
        }

        private bool IsImperative(string summary)
        {
            // Basic check: first word is a verb (not past tense)
            var firstWord = summary.Split(' ').FirstOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(firstWord)) return false;
            // List of common past tense endings
            if (firstWord.EndsWith("ed")) return false;
            return true;
        }
    }
}
