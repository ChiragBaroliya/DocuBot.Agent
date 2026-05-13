using System.Threading.Tasks;

namespace DocuBot.AI.Interfaces
{
    public interface IFunctionalDocGenerator
    {
        /// <summary>
        /// Generates documentation for a code diff (e.g., for a commit or PR).
        /// </summary>
        Task<string> GenerateForDiffAsync(string diff);

        /// <summary>
        /// Generates documentation for the entire codebase.
        /// </summary>
        Task<string> GenerateForCodebaseAsync(string solutionPath);
    }
}
