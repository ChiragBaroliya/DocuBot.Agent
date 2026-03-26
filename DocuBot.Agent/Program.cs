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

Console.WriteLine($"Current branch: {branch}");

// ✅ Branch validation
var ignoredBranches = new[] { "master", "main", "develop" };
if (!ignoredBranches.Contains(branch.ToLower()) && !validator.ValidateBranchName(branch))
{
    Console.WriteLine("ERROR: Invalid branch name (use feature/*, bugfix/*, hotfix/*).");
    Environment.Exit(1);
}

if (!string.IsNullOrWhiteSpace(stagedDiff))
{
    Console.WriteLine("🤖 Running Code Quality & Security Review...");
    string codeReviewReport = await aiService.GenerateCodeReviewAsync(stagedDiff);
    string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "CodeReviewReport.md");
    File.WriteAllText(reportPath, codeReviewReport);
    
    if (codeReviewReport.Contains("Status: PASS"))
    {
        Console.WriteLine($"✅ Code review passed. Report saved to {reportPath}");
    }
    else
    {
        Console.WriteLine($"\n❌ Code Review found HIGH or CRITICAL issues.");
        Console.WriteLine($"Please check {reportPath} for details.");
        Environment.Exit(1);
    }
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

bool isAiSuggested = commitMsg.StartsWith("[AI] ", StringComparison.OrdinalIgnoreCase);

if (isAiSuggested)
{
    Console.WriteLine("✅ Developer used the AI-suggested commit message. Bypassing semantic validation.");
    
    // Remove prefix before committing
    commitMsg = commitMsg.Substring(5).Trim();
}
else
{
    if (!validator.ValidateCommitMessage(commitMsg))
    {
        Console.WriteLine("\n❌ Invalid commit message format.");
        await SuggestAndExitAsync();
    }

    Console.WriteLine("🤖 Validating commit message semantics with AI...");
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
        Console.WriteLine($"\n✅ Commit successful: {result}");
        Environment.Exit(0);
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ Commit failed.");
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

        Console.WriteLine("\n💡 Suggested commit message (Copy and paste including [AI]):");
        Console.WriteLine($"[AI] {suggestedCommitMsg}");

        // ❗ STOP execution here (DO NOT COMMIT)
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

    var lines = aiResponse.Split('\n');

    foreach (var line in lines)
    {
        var trimmed = line.Trim('`', ' ', '\r');

        if (allowedTypes.Any(type => trimmed.StartsWith(type, StringComparison.OrdinalIgnoreCase)))
        {
            return trimmed;
        }
    }

    return string.Empty;
}