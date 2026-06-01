using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using DocuBot.Application.Interfaces;
using DocuBot.Domain.Interfaces;
using DocuBot.Domain.Services;
using DocuBot.Infrastructure.Services;
using DocuBot.Infrastructure.Utils;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Net.Http;

var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}
else
{
    Env.Load();
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory,
    Args = args
});

// Reduce noise logs
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IAiModelService, WebApiAiModelService>();
builder.Services.AddSingleton<IGitService, GitExecutor>();
builder.Services.AddSingleton<IGitValidator, GitValidator>();
builder.Services.AddLogging();

builder.Services.AddHttpClient<DocuBot.Agent.Services.IMcpService, DocuBot.Agent.Services.McpService>();
builder.Services.AddSingleton<DocuBot.Agent.Services.IDocumentationOrchestrator, DocuBot.Agent.Services.DocumentationOrchestrator>();

var app = builder.Build();


var gitService = app.Services.GetRequiredService<IGitService>();
var validator = app.Services.GetRequiredService<IGitValidator>();
var aiService = app.Services.GetRequiredService<IAiModelService>();

string branch = gitService.GetCurrentBranch();
string stagedDiff = gitService.GetStagedDiff();
string commitMsg = string.Empty;

// ✅ Branch validation
var ignoredBranches = new[] { "master", "main", "develop" };
if (!ignoredBranches.Contains(branch.ToLower()) && !validator.ValidateBranchName(branch.ToLower()))
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
    string htmlReport = await aiService.GenerateCodeReviewHtmlReportAsync(stagedDiff);
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
    if (!validator.ValidateCommitMessage(commitMsg))
    {
        await SuggestAndExitAsync();
    }

    bool isSemanticallyValid = await aiService.ValidateCommitMessageAsync(commitMsg, stagedDiff);

    if (!isSemanticallyValid)
    {
        Console.WriteLine("\n❌ Commit message does not accurately describe the changes.");
        await SuggestAndExitAsync();
    }
}

async Task SuggestAndExitAsync()
{
    try
    {
        string aiResponse = await aiService.GenerateCommitMessageAsync(stagedDiff);
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

string ExtractValidCommitMessage(string aiResponse)
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