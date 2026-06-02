using DocuBot.Application.Interfaces;
using DocuBot.Domain.Interfaces;
using DocuBot.Domain.Services;
using DocuBot.Infrastructure.Services;
using DocuBot.Agent.Services;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

// Core Services
builder.Services.AddSingleton<IAiModelService, WebApiAiModelService>();
builder.Services.AddSingleton<IGitService, GitExecutor>();
builder.Services.AddSingleton<IGitValidator, GitValidator>();
builder.Services.AddLogging();

// Agent Services
builder.Services.AddHttpClient<IMcpService, McpService>();
builder.Services.AddSingleton<IDocumentationOrchestrator, DocumentationOrchestrator>();
builder.Services.AddSingleton<ICommitWorkflowExecutor, CommitWorkflowExecutor>();

var app = builder.Build();

var executor = app.Services.GetRequiredService<ICommitWorkflowExecutor>();
await executor.ExecuteAsync(args);