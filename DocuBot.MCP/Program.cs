using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DocuBot.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using DocuBot.MCP.Services;
using DocuBot.Infrastructure.Services;
using DocuBot.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IMcpService, McpService>();
builder.Services.AddSingleton<IAiModelService, OllamaService>();
builder.Services.AddSingleton<DocumentationOrchestrator>();
builder.Services.AddLogging();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
