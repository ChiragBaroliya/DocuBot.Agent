using System;
using System.IO;
using System.Linq;
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

            // ✅ Branch validation
            var ignoredBranches = new[] { "master", "main", "develop" };
            if (!ignoredBranches.Contains(branch.ToLower()) && !_validator.ValidateBranchName(branch.ToLower()))
            {
                Console.WriteLine("ERROR: Invalid branch name (use feature/*, bugfix/*, hotfix/*).");
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

            // Accept any commit message starting with [AI], [AI] , [AI]:, [AI] :, etc.
            bool isAiSuggested = false;
            if (commitMsg.StartsWith("[AI]", StringComparison.OrdinalIgnoreCase))
            {
                // Remove [AI], [AI] , [AI]:, [AI] :, etc. prefix
                var aiPrefix = "[AI]";
                commitMsg = commitMsg.Substring(aiPrefix.Length).TrimStart();
                if (commitMsg.StartsWith(":"))
                {
                    commitMsg = commitMsg.Substring(1).TrimStart();
                }
                isAiSuggested = true;
            }

            if (isAiSuggested)
            {
                // Accept AI-suggested commit message as valid, skip further validation and suggestion
            }
            else
            {
                if (!_validator.ValidateCommitMessage(commitMsg))
                {
                    await SuggestAndExitAsync();
                }

                bool isSemanticallyValid = await _aiService.ValidateCommitMessageAsync(commitMsg, stagedDiff);

                if (!isSemanticallyValid)
                {
                    Console.WriteLine("\n❌ Commit message does not accurately describe the changes.");
                    await SuggestAndExitAsync();
                }
            }
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

                Console.WriteLine($"[AI] {suggestedCommitMsg}");

                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ AI generation failed.");
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

            // Return the rest of the response starting from the valid line
            return string.Join("\n", lines.Skip(startIndex)).Trim();
        }
    }
}
