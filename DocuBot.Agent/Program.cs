using DocuBot.Application.Interfaces;
using DocuBot.Infrastructure.Services;
using DocuBot.Domain.Services;
using DocuBot.AI.Services;
using DocuBot.AI.Options;
using DocuBot.AI.Interfaces;
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

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IOpenAIService, DocuBot.Agent.Services.OllamaService>();
builder.Services.AddSingleton<IGitService, GitExecutor>();
builder.Services.AddSingleton<IGitValidator, GitValidator>();
builder.Services.AddLogging();
builder.Services.AddHttpClient<DocuBot.Agent.Services.IMcpService, DocuBot.Agent.Services.McpService>();
builder.Services.AddSingleton<DocuBot.Agent.Services.IDocumentationOrchestrator, DocuBot.Agent.Services.DocumentationOrchestrator>();

var app = builder.Build();
var gitService = app.Services.GetRequiredService<IGitService>();
var validator = app.Services.GetRequiredService<IGitValidator>();
var aiService = app.Services.GetRequiredService<IOpenAIService>();
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

Console.WriteLine($"Commit message: {commitMsg}");


if (!validator.ValidateCommitMessage(commitMsg))
{
    Console.WriteLine("Generating AI suggestion...");

    try
    {
        string suggestedCommitMsg = await aiService.GenerateCommitMessageAsync(stagedDiff);
        Console.WriteLine("Suggested commit message:");
        Console.WriteLine(suggestedCommitMsg);

        // Generate markdown documentation for staged changes and save to docs/commit-doc.md
        try
        {
            string documentation = await aiService.GenerateDocumentationAsync(stagedDiff);
            string docPath = Path.Combine("docs", "commit-doc.md");
            Directory.CreateDirectory("docs");
            File.AppendAllText(docPath, documentation + Environment.NewLine);
            Console.WriteLine($"\nMarkdown documentation generated at {docPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to generate markdown documentation.");
            Console.WriteLine(ex.Message);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("AI generation failed.");
        Console.WriteLine("FULL ERROR:");
        Console.WriteLine(ex.ToString());   // This prints full message + stack trace
    }

    Console.WriteLine("Please review the updated commit message.");
    Environment.Exit(1);
}


//Console.WriteLine("\nFiles staged for commit:");
//var changedFiles = stagedDiff.Split('\n')
//    .Where(line => line.StartsWith("+++ b/") || line.StartsWith("--- a/"))
//    .Select(line => line.Replace("+++ b/", "").Replace("--- a/", ""))
//    .Distinct()
//    .Where(f => !string.IsNullOrWhiteSpace(f) && f != "/dev/null")
//    .ToList();
//foreach (var file in changedFiles)
//{
//    Console.WriteLine("- " + file);
//}



