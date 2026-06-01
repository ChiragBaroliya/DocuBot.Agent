using System;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DocuBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocuBot.Infrastructure.Services
{
    public class SecretsManagerService : ISecretsManagerService
    {
        private readonly AmazonSecretsManagerClient _client;

        public SecretsManagerService(AWSCredentials credentials, Amazon.RegionEndpoint region)
        {
            _client = new AmazonSecretsManagerClient(credentials, region);
        }

        public SecretsManagerService(IConfiguration configuration)
        {
            var credentials = AwsCredentialProvider.GetCredentials(configuration);
            var region = AwsCredentialProvider.GetRegion(configuration);
            _client = new AmazonSecretsManagerClient(credentials, region);
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName
            };

            try
            {
                var response = await _client.GetSecretValueAsync(request);
                return response.SecretString;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching secret '{secretName}' from AWS Secrets Manager: {ex.Message}", ex);
            }
        }
    }
}
