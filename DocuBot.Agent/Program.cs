using DocuBot.Application.Interfaces;
using DocuBot.Infrastructure.Services;
using DocuBot.Domain.Services;
using DocuBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DotNetEnv;
using System.Dynamic;
using Amazon.Runtime;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

// Reduce noise logs
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

builder.Services.AddHttpClient();

var credentialService = new AwsCredentialService();
var awsCredentials = await credentialService.GetCredentialsAsync();
var awsRegion = AwsCredentialProvider.GetRegion();

builder.Services.AddSingleton<IAiModelService>(sp =>
{
    return new AmazonBedrockService(awsCredentials, awsRegion);
});

builder.Services.AddSingleton<ISecretsManagerService>(sp => new SecretsManagerService(awsCredentials, awsRegion));
builder.Services.AddSingleton<IGitService, GitExecutor>();
builder.Services.AddSingleton<IGitValidator, GitValidator>();
builder.Services.AddLogging();

builder.Services.AddHttpClient<DocuBot.Agent.Services.IMcpService, DocuBot.Agent.Services.McpService>();
builder.Services.AddSingleton<DocuBot.Agent.Services.IDocumentationOrchestrator, DocuBot.Agent.Services.DocumentationOrchestrator>();

var app = builder.Build();

// --- AWS Connection Self-Test ---
//Console.WriteLine("🧪 Running AWS Connectivity Self-Test...");
//var ai = app.Services.GetRequiredService<IAiModelService>();
//var secrets = app.Services.GetRequiredService<ISecretsManagerService>();

//Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development"}");
//Console.WriteLine($"Region: {AwsCredentialProvider.GetRegion().SystemName}");

//try
//{
//    Console.WriteLine("📡 Testing Amazon Bedrock...");
//    var testResult = await ai.GetResponseAsync("us.meta.llama3-1-8b-instruct-v1:0", "Say 'AWS Bedrock Connection Successful'");
//    if (testResult.Contains("[AmazonBedrockService Error]"))
//    {
//        Console.WriteLine($"❌ Bedrock Test Failed: {testResult}");
//    }
//    else
//    {
//        Console.WriteLine($"✅ Bedrock Test Successful! Response: {testResult.Trim()}");
//    }
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"❌ Bedrock Test Failed with Exception: {ex.Message}");
//}

//if (!args.Contains("--continue")) Environment.Exit(0);

// Optional: Load additional secrets from AWS Secrets Manager if a secret name is provided
var awsSecretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
if (!string.IsNullOrEmpty(awsSecretName))
{
    var secretsService = app.Services.GetRequiredService<ISecretsManagerService>();
    try
    {
        Console.WriteLine($"🔐 Loading configuration from AWS Secrets Manager: {awsSecretName}...");
        var secretJson = await secretsService.GetSecretAsync(awsSecretName);
        // Note: You might want to parse this JSON and set environment variables or update IConfiguration
        // For now, we just acknowledge it's available.
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning: Could not load secrets from AWS: {ex.Message}");
    }
}

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