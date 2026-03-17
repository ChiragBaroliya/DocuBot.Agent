using DocuBot.Application.Interfaces;
using DocuBot.Infrastructure.Services;
using DocuBot.Domain.Services;
using DocuBot.AI.Services;
using DocuBot.AI.Options;
using DocuBot.AI.Interfaces;
using DocuBot.CLI.Commands;
using DocuBot.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using DocuBot.Domain.Interfaces;
using System.Linq;
using System;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConventionalCommitGenerator, ConventionalCommitGenerator>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiModelService, OllamaService>();
builder.Services.AddSingleton<IGitService, GitExecutor>();
builder.Services.AddSingleton<IGitValidator, GitValidator>();
builder.Services.AddLogging();

var app = builder.Build();

var command = args.FirstOrDefault();

switch (command)
{
    case "validate":
        var validateCmd = app.Services.GetRequiredService<ValidateCommand>();
        validateCmd.Execute();
        break;
    case "suggest-commit":
        var gitService = app.Services.GetRequiredService<IGitService>();
        var aiService = app.Services.GetRequiredService<IAiModelService>();
        var diff = gitService.GetStagedDiff();
        var commitMsg = await aiService.GenerateCommitMessageAsync(diff);
        Console.WriteLine(commitMsg);
        break;
    case "pre-push":
        // Implement pre-push logic
        break;
    default:
        Console.WriteLine("Unknown command.");
        break;
}
