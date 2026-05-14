using System;
using Amazon.Runtime;
using DocuBot.Infrastructure.Services;
using System.Threading.Tasks;
using Xunit;

namespace DocuBot.Tests.Services
{
    public class AwsCredentialProviderTests : IDisposable
    {
        public AwsCredentialProviderTests()
        {
            // Clear relevant env vars before each test
            Environment.SetEnvironmentVariable("ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
            Environment.SetEnvironmentVariable("AWS_ROLE_ARN", null);
            Environment.SetEnvironmentVariable("AWS_REGION", null);
            Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN", null);
        }

        public void Dispose()
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
            Environment.SetEnvironmentVariable("AWS_ROLE_ARN", null);
            Environment.SetEnvironmentVariable("AWS_REGION", null);
            Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN", null);
        }

        [Fact]
        public void GetCredentials_ShouldReturnBasicCredentials_WhenInDevelopmentWithKeys()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "AKIA...");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "SECRET...");

            // Act
            var credentials = AwsCredentialProvider.GetCredentials();

            // Assert
            Assert.IsType<BasicAWSCredentials>(credentials);
        }

        [Fact]
        public void GetCredentials_ShouldReturnSessionCredentials_WhenTokenProvided()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "ASIA...");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "SECRET...");
            Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN", "TOKEN...");

            // Act
            var credentials = AwsCredentialProvider.GetCredentials();

            // Assert
            Assert.IsType<SessionAWSCredentials>(credentials);
        }

        [Fact]
        public void GetCredentials_ShouldReturnAssumeRoleCredentials_WhenRoleArnProvided()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Production");
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "");
            Environment.SetEnvironmentVariable("AWS_REGION", "eu-central-1");
            Environment.SetEnvironmentVariable("AWS_ROLE_ARN", "arn:aws:iam::584949450016:role/docubot-developer-role");

            // Act
            var credentials = AwsCredentialProvider.GetCredentials();

            // Assert
            Assert.IsType<AssumeRoleAWSCredentials>(credentials);
        }

        [Fact]
        public void GetRegion_ShouldReturnDefault_WhenNoEnvVar()
        {
            // Act
            var region = AwsCredentialProvider.GetRegion();

            // Assert
            Assert.Equal("us-east-1", region.SystemName);
        }

        [Fact]
        public void GetRegion_ShouldReturnEnvValue_WhenSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("AWS_REGION", "us-west-2");

            // Act
            var region = AwsCredentialProvider.GetRegion();

            // Assert
            Assert.Equal("us-west-2", region.SystemName);
        }
        [Fact(Skip = "Manual Integration Test - Requires real AWS credentials")]
        public async Task GetResponseAsync_RealConnectionTest()
        {
            // Arrange
            // Ensure you have valid credentials set in your environment or .env
            var service = new AmazonBedrockService();

            // Act
            var result = await service.GetResponseAsync("meta.llama3-3-70b-instruct-v1:0", "Say 'Test Successful'");

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("[AmazonBedrockService Error]", result);
            Console.WriteLine($"Bedrock Response: {result}");
        }
    }
}
