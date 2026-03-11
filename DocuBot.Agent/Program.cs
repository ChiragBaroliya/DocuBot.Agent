using DocuBot.Application.Interfaces;
using DocuBot.Infrastructure.Services;
using DocuBot.Domain.Services;
using DocuBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System;
using System.Linq;


var builder = Host.CreateApplicationBuilder(args);

// Suppress info-level logs from Microsoft and System namespaces
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

builder.Services.AddSingleton<IAiModelService, GoogleGeminiAPIService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiModelService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    return new GoogleGeminiAPIService(httpClient, string.Empty);
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
var docOrchestrator = app.Services.GetRequiredService<DocuBot.Agent.Services.IDocumentationOrchestrator>();


string branch = gitService.GetCurrentBranch();
string stagedDiff = gitService.GetStagedDiff();



Console.WriteLine($"Current branch: {branch}");
if (!validator.ValidateBranchName(branch))
{
    Console.WriteLine("ERROR: Branch name does not follow required convention (feature/*, bugfix/*, hotfix/*). Push blocked.");
    Environment.Exit(1);
}

string commitMsgFile = args.Length > 0 ? args[0] : "";
string commitMsg = "";

if (!string.IsNullOrEmpty(commitMsgFile) && File.Exists(commitMsgFile))
{
    commitMsg = File.ReadAllText(commitMsgFile).Trim();
}


if (!validator.ValidateCommitMessage(commitMsg))
{
    try
    {
        string suggestedCommitMsg = await aiService.GenerateCommitMessageAsync(stagedDiff);
        Console.WriteLine("Suggested commit message:");
        Console.WriteLine(suggestedCommitMsg);

        /*
        // Generate markdown documentation for staged changes and save to docs/commit-doc.md
        try
        {
            string documentation = await aiService.GenerateDocumentationAsync(stagedDiff);
            string docPath = Path.Combine("docs", "commit-doc.md");
            Directory.CreateDirectory("docs");

            // Compute hash of stagedDiff
            string stagedDiffHash = "";
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(stagedDiff));
                stagedDiffHash = BitConverter.ToString(hashBytes).Replace("-", "");
            }

            bool alreadyDocumented = false;
            if (File.Exists(docPath))
            {
                var docContent = File.ReadAllText(docPath);
                if (docContent.Contains(stagedDiffHash))
                {
                    alreadyDocumented = true;
                }
            }

            if (!alreadyDocumented)
            {
                File.AppendAllText(docPath, $"<!-- DIFF_HASH:{stagedDiffHash} -->\n" + documentation + Environment.NewLine);
                Console.WriteLine($"\nMarkdown documentation generated at {docPath}");
                // Automatically stage the documentation file
                try
                {
                    gitService.StageFile(docPath);
                    Console.WriteLine($"{docPath} staged for commit.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to stage {docPath}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"\nDocumentation for staged diff already exists in {docPath}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to generate markdown documentation.");
            Console.WriteLine(ex.Message);
        }
        */
    }
    catch (Exception ex)
    {
        Console.WriteLine("AI generation failed.");
        Console.WriteLine("FULL ERROR:");
        Console.WriteLine(ex.ToString());   // This prints full message + stack trace
    }

    Environment.Exit(1);
}

// If commit message is valid, commit staged files
//try
//{
//    var commitResult = gitService.CommitStagedFiles(commitMsg);
//    Console.WriteLine($"\nCommit successful: {commitResult}");
//}
//catch (Exception ex)
//{
//    Console.WriteLine("Commit failed.");
//    Console.WriteLine(ex.Message);
//    Environment.Exit(1);
//}





