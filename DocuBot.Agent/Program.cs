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

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddSingleton<IConventionalCommitGenerator, ConventionalCommitGenerator>();
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddSingleton<IOpenAIService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAIOptions>>();
    var logger = sp.GetRequiredService<ILogger<OpenAIService>>();
    var generator = sp.GetRequiredService<IConventionalCommitGenerator>();
    return new OpenAIService(httpClientFactory.CreateClient(), options, logger, generator);
});
builder.Services.AddSingleton<IGitService, GitExecutor>();
builder.Services.AddSingleton<IGitValidator, GitValidator>();
builder.Services.AddLogging();

var app = builder.Build();
var gitService = app.Services.GetRequiredService<IGitService>();
var validator = app.Services.GetRequiredService<IGitValidator>();
var aiService = app.Services.GetRequiredService<IOpenAIService>();

string branch = gitService.GetCurrentBranch();
string stagedDiff = gitService.GetStagedDiff();

Console.WriteLine($"Current branch: {branch}");
if (!validator.ValidateBranchName(branch))
{
    Console.WriteLine("ERROR: Branch name does not follow required convention (feature/*, bugfix/*, hotfix/*). Push blocked.");
    Environment.Exit(1);
}

Console.WriteLine("Validating commit message...");
Console.WriteLine("Enter your commit message:");
string commitMsg = Console.ReadLine() ?? string.Empty;
if (!validator.ValidateCommitMessage(commitMsg))
{
    Console.WriteLine("ERROR: Commit message does not follow Conventional Commit format. Push blocked.");
    Environment.Exit(1);
}

Console.WriteLine("Suggesting AI-generated Conventional Commit message...");
string aiCommitMsg = await aiService.GenerateCommitMessageAsync(stagedDiff);
Console.WriteLine($"AI Suggestion: {aiCommitMsg}");

Console.WriteLine("All checks passed. You may push your changes.");
