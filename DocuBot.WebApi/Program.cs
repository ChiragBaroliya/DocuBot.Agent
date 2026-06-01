using DocuBot.Application.Interfaces;
using DocuBot.Infrastructure.Services;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load environmental variables from .env file
var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}
else
{
    // Try current directory as well
    var currentDirEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(currentDirEnv))
    {
        Env.Load(currentDirEnv);
    }
    else
    {
        Env.Load();
    }
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure AWS Credentials and Bedrock Service
var credentialService = new AwsCredentialService(builder.Configuration);
var awsCredentials = await credentialService.GetCredentialsAsync();
var awsRegion = AwsCredentialProvider.GetRegion(builder.Configuration);

builder.Services.AddSingleton<IAiModelService>(sp =>
{
    return new AmazonBedrockService(awsCredentials, awsRegion, builder.Configuration);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
