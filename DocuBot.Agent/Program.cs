using DocuBot.Application.Interfaces;
using DocuBot.Infrastructure.Services;
using DocuBot.Domain.Services;
using DocuBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DotNetEnv;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

// Reduce noise logs
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IAiModelService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var apiKey = Environment.GetEnvironmentVariable("GROQAI_API_KEY");
    return new GroqAIService(httpClient, apiKey ?? string.Empty);
});

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
if (!ignoredBranches.Contains(branch.ToLower()) && !validator.ValidateBranchName(branch))
{
    Console.WriteLine("ERROR: Invalid branch name (use feature/*, bugfix/*, hotfix/*).");
    Environment.Exit(1);
}

// Read commit message from file (if provided) or directly from args
string commitMsgInput = args.Length > 0 ? args[0] : "";

bool isHookMode = false;

if (!string.IsNullOrEmpty(commitMsgInput) && File.Exists(commitMsgInput))
{
    commitMsg = File.ReadAllText(commitMsgInput).Trim();
    isHookMode = true;
}
else if (!string.IsNullOrEmpty(commitMsgInput))
{
    commitMsg = commitMsgInput.Trim();
}

/*
bool skipReview = commitMsg.Contains("[SKIP REVIEW]", StringComparison.OrdinalIgnoreCase);

if (!skipReview && !string.IsNullOrWhiteSpace(stagedDiff))
{
    Console.WriteLine("🤖 Running OWASP Security Review...");
    string codeReviewReport = await aiService.GenerateCodeReviewAsync(stagedDiff);
    string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "CodeReviewReport.md");
    File.WriteAllText(reportPath, codeReviewReport);
    
    bool isPassed = codeReviewReport.Contains("Status: PASS", StringComparison.OrdinalIgnoreCase);
    
    if (isPassed)
    {
        Console.WriteLine($"✅ Code review passed. Report saved to {reportPath}");
    }
    else
    {
        Console.WriteLine($"\n❌ Code Review found potential HIGH or CRITICAL OWASP issues.");
        Console.WriteLine($"--- AI Response (Status check failed) ---");
        Console.WriteLine(codeReviewReport.Length > 200 ? codeReviewReport.Substring(0, 200) + "..." : codeReviewReport);
        Console.WriteLine($"------------------------------------------");
        Console.WriteLine($"Please check {reportPath} for details.");
        Console.WriteLine("\n💡 To bypass this check for emergency commits, add [SKIP REVIEW] to your commit message.");
        
        await SuggestAndExitAsync();
        Environment.Exit(1);
    }
}
*/


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

// Finalize Commit Processing
try
{
    if (isHookMode)
    {
        // Git is already running a commit. We just update the message file and let Git finish.
        File.WriteAllText(commitMsgInput, commitMsg);
        Environment.Exit(0);
    }
    else
    {
        // Standalone mode: we need to trigger the git commit ourselves.
        var result = gitService.CommitStagedFiles(commitMsg);
        Environment.Exit(0);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Environment.Exit(1);
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