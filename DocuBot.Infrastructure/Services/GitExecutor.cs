using System;
using System.Diagnostics;
using DocuBot.Application.Interfaces;
using DocuBot.Domain.Services;
using Microsoft.Extensions.Logging;

namespace DocuBot.Infrastructure.Services
{
    public class GitExecutor : IGitService
    {
        private readonly ILogger<GitExecutor> _logger;
        private readonly GitValidator _validator;

        public GitExecutor(ILogger<GitExecutor> logger)
        {
            _logger = logger;
            _validator = new GitValidator();
        }

        public string GetStagedDiff()
        {
            return RunGitCommand("git diff --cached");
        }

        public string GetCurrentBranch()
        {
            return RunGitCommand("git rev-parse --abbrev-ref HEAD").Trim();
        }


        public bool ValidateBranch(string branchName)
        {
            return _validator.ValidateBranchName(branchName);
        }

        public bool ValidateCommit(string commitMessage)
        {
            return _validator.ValidateCommitMessage(commitMessage);
        }
        
        public string CommitStagedFiles(string commitMessage)
        {
            // Commit staged files with the provided commit message
            return RunGitCommand($"git commit -m \"{commitMessage.Replace("\"", "'")}\"");
        }

        public string GetLastCommitDiff()
        {
            // Returns the diff of the last commit (HEAD)
            return RunGitCommand("git show HEAD");
        }

        private string RunGitCommand(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running git command: {Command}", command);
                throw;
            }
        }
        public void StageFile(string filePath)
        {
            RunGitCommand($"git add \"{filePath}\"");
        }
    }
}
