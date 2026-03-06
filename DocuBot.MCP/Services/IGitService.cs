namespace DocuBot.MCP.Services
{
    public interface IGitService
    {
        string GetStagedDiff();
        string GetCurrentBranch();
        bool ValidateBranch(string branchName);
        bool ValidateCommit(string commitMessage);
    }
}
