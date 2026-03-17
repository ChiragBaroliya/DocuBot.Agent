namespace DocuBot.Application.Interfaces
{
    public interface IGitService
    {
        string GetStagedDiff();
        string GetCurrentBranch();
        string GetLastCommitDiff();
        bool ValidateBranch(string branchName);
        bool ValidateCommit(string commitMessage);
        string CommitStagedFiles(string commitMessage);
        void StageFile(string filePath);
    }
}
