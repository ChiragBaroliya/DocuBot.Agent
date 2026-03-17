namespace DocuBot.AI.Interfaces
{
    public interface IConventionalCommitGenerator
    {
        /// <summary>
        /// Generates a strictly valid Conventional Commit message from diff and AI suggestion.
        /// </summary>
        /// <param name="aiSuggestion">AI-generated commit message suggestion</param>
        /// <param name="diff">Git diff</param>
        /// <returns>Valid Conventional Commit message or strict fallback</returns>
        string Generate(string aiSuggestion, string diff);
    }
}
