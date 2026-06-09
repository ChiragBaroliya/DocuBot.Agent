using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocuBot.Application.Interfaces;
using DocuBot.Domain.Interfaces;
using DocuBot.Infrastructure.Utils;

namespace DocuBot.Agent.Services
{
    public class CommitWorkflowExecutor : ICommitWorkflowExecutor
    {
        private readonly IGitService _gitService;
        private readonly IGitValidator _validator;
        private readonly IAiModelService _aiService;

        public CommitWorkflowExecutor(
            IGitService gitService,
            IGitValidator validator,
            IAiModelService aiService)
        {
            _gitService = gitService;
            _validator = validator;
            _aiService = aiService;
        }

        public async Task ExecuteAsync(string[] args)
        {
            string branch = _gitService.GetCurrentBranch();
            string stagedDiff = _gitService.GetStagedDiff();
            string commitMsg = string.Empty;

            // Branch validation
            var ignoredBranches = new[] { "master", "main", "develop" };
            if (!ignoredBranches.Contains(branch.ToLower()) && !_validator.ValidateBranchName(branch.ToLower()))
            {
                Console.WriteLine("Invalid branch name. Use feature/*, bugfix/*, or hotfix/*.");
                Console.WriteLine($"Current branch: {branch}");
                Environment.Exit(1);
            }

            // Read commit message from file (if provided) or directly from args
            string commitMsgInput = args.Length > 0 ? args[0] : "";

            if (!string.IsNullOrEmpty(commitMsgInput) && File.Exists(commitMsgInput))
            {
                commitMsg = File.ReadAllText(commitMsgInput).Trim();
            }
            else if (!string.IsNullOrEmpty(commitMsgInput))
            {
                commitMsg = commitMsgInput.Trim();
            }

            bool skipReview = commitMsg.Contains("[SKIP REVIEW]", StringComparison.OrdinalIgnoreCase);

            if (!skipReview && !string.IsNullOrWhiteSpace(stagedDiff))
            {
                // Get the HTML report from your AI service
                string htmlReport = await _aiService.GenerateCodeReviewHtmlReportAsync(stagedDiff);
                htmlReport = AiResponseCleaner.RemoveCodeFences(htmlReport);

                var downloadsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads");
                var reportPath = Path.Combine(downloadsPath, "CodeReviewReport.html");
                File.WriteAllText(reportPath, htmlReport);

                // Try to open the HTML file automatically
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = reportPath,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch { /* Ignore open errors */ }
            }

            // generate functional README update in parallel (non-blocking)
            await UpdateFunctionalReadmeAsync(stagedDiff);

            // validate commit message and return ai suggested commit message

            if (!_validator.ValidateCommitMessage(commitMsg))
            {
                Console.WriteLine("Your commit message format is invalid. Please use the AI-suggested message below:");
                await SuggestAndExitAsync();
            }

            bool isSemanticallyValid = await _aiService.ValidateCommitMessageAsync(commitMsg, stagedDiff);

            if (!isSemanticallyValid)
            {
                Console.WriteLine("Your commit message does not match the staged changes. Please use the AI-suggested message below:");
                await SuggestAndExitAsync();
            }

            
        }

        private async Task UpdateFunctionalReadmeAsync(string stagedDiff)
        {
            if (string.IsNullOrWhiteSpace(stagedDiff))
            {
                return;
            }

            try
            {
                var repositoryRoot = ResolveRepositoryRoot();
                var functionalReadmeRelativePath = "README.md";
                var functionalReadmeAbsolutePath = Path.Combine(repositoryRoot, functionalReadmeRelativePath);

                var functionalInput = BuildReadmeSourceInput(stagedDiff);

                var readmeContent = await _aiService.GenerateMasterFunctionalReadmeAsync(functionalInput);
                if (string.IsNullOrWhiteSpace(readmeContent))
                {
                    return;
                }

                readmeContent = AiResponseCleaner.RemoveCodeFences(readmeContent).Trim();

                await File.WriteAllTextAsync(functionalReadmeAbsolutePath, readmeContent.Trim());
                _gitService.StageFile(functionalReadmeRelativePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Functional README update failed: {ex.Message}");
            }
        }

        private static string BuildReadmeSourceInput(string stagedDiff)
        {
            const int maxDiffChars = 12000;
            var trimmedDiff = stagedDiff.Length > maxDiffChars
                ? stagedDiff[..maxDiffChars]
                : stagedDiff;

            return trimmedDiff;
        }

        private static string ResolveRepositoryRoot()
        {
            var candidates = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var candidate in candidates)
            {
                var current = new DirectoryInfo(candidate);
                while (current != null)
                {
                    var hasGitMarker = Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                                       File.Exists(Path.Combine(current.FullName, ".git"));
                    var hasSolutionFile = Directory.GetFiles(current.FullName, "*.sln", SearchOption.TopDirectoryOnly).Length > 0;

                    if (hasGitMarker || hasSolutionFile)
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }
            }

            return Directory.GetCurrentDirectory();
        }

        private async Task SuggestAndExitAsync()
        {
            try
            {
                string stagedDiff = _gitService.GetStagedDiff();
                string aiResponse = await _aiService.GenerateCommitMessageAsync(stagedDiff);
                string suggestedCommitMsg = ExtractValidCommitMessage(aiResponse);

                if (string.IsNullOrEmpty(suggestedCommitMsg))
                {
                    suggestedCommitMsg = aiResponse.Trim();
                }

                //Console.WriteLine("AI Suggested commit message:");
                Console.WriteLine(suggestedCommitMsg);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Suggested commit message is currently unavailable.");
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }

        private string ExtractValidCommitMessage(string aiResponse)
        {
            var allowedTypes = new[]
            {
                "feat:", "fix:", "bug:", "chore:", "docs:",
                "style:", "refactor:", "perf:", "test:", "build:", "ci:", "revert:"
            };

            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Find the first line that starts with an allowed type
            int startIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim('`', ' ', '\r');
                if (allowedTypes.Any(type => trimmed.StartsWith(type, StringComparison.OrdinalIgnoreCase)))
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex == -1) return string.Empty;

            // Return only the first valid conventional commit subject line
            return lines[startIndex].Trim('`', ' ', '\r').Trim();
        }
    }
}
