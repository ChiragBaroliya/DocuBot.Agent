using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.Configuration;

namespace DocuBot.Infrastructure.Services
{
    /// <summary>
    /// Service for obtaining AWS credentials and assuming roles for secure AWS access.
    /// Supports both Development (via SSO/Profiles) and Production (via IAM Roles) environments.
    /// </summary>
    public class AwsCredentialService
    {
        private readonly string _environment;
        private readonly string _roleArn;
        private readonly string _profileName;
        private readonly RegionEndpoint _region;

        public AwsCredentialService(IConfiguration configuration)
        {
            _environment = configuration["Environment"] ?? "Development";
            var awsSection = configuration.GetSection("AWS");
            _profileName = awsSection["Profile"] ?? string.Empty;
            _roleArn = awsSection["RoleArn"] ?? string.Empty;
            var regionName = awsSection["Region"] ?? "eu-central-1";
            _region = RegionEndpoint.GetBySystemName(regionName);
        }


        public async Task<AWSCredentials> GetCredentialsAsync(CancellationToken cancellationToken = default)
        {
            bool isProduction = _environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

            // If no Role ARN is provided, return base credentials
            if (string.IsNullOrEmpty(_roleArn) || !isProduction)
            {
                return GetBaseCredentials(isProduction);
            }

            return await AssumeRoleAsync(isProduction, cancellationToken).ConfigureAwait(false);
        }

        private async Task<AWSCredentials> AssumeRoleAsync(bool isProduction, CancellationToken cancellationToken = default)
        {
            using var stsClient = GetAmazonSecurityTokenService(isProduction);

            var assumeRoleRequest = new AssumeRoleRequest
            {
                RoleArn = _roleArn,
                RoleSessionName = "DocuBotAgentSession",
            };

            try
            {
                var assumeRoleResponse = await stsClient.AssumeRoleAsync(assumeRoleRequest, cancellationToken).ConfigureAwait(false);
                var credentials = assumeRoleResponse.Credentials;

                return new SessionAWSCredentials(
                    credentials.AccessKeyId,
                    credentials.SecretAccessKey,
                    credentials.SessionToken
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error assuming role {_roleArn}: {ex.Message}", ex);
            }
        }

        private AmazonSecurityTokenServiceClient GetAmazonSecurityTokenService(bool isProduction)
        {
            if (isProduction)
            {
                // In Production, use the default credential chain (e.g., IAM Instance Profile)
                return new AmazonSecurityTokenServiceClient(_region);
            }
            else
            {
                var sourceCredentials = GetBaseCredentials(false);
                return new AmazonSecurityTokenServiceClient(sourceCredentials, _region);
            }
        }

        internal AWSCredentials GetBaseCredentials(bool isProduction)
        {
            if (isProduction)
            {
                // Use default credential chain (e.g., IAM role on EC2, ECS, etc.)
                return FallbackCredentialsFactory.GetCredentials();
            }

            // Always use AWS Profile for development (SSO or local profile)
            if (!string.IsNullOrEmpty(_profileName))
            {
                var chain = new CredentialProfileStoreChain();
                if (chain.TryGetAWSCredentials(_profileName, out var profileCredentials))
                {
                    return profileCredentials;
                }
            }

            // Fallback to default chain if profile not found
            return FallbackCredentialsFactory.GetCredentials();
        }
    }

    /// <summary>
    /// Legacy static provider updated to use the new service.
    /// This ensures backward compatibility with existing code.
    /// </summary>
    public static class AwsCredentialProvider
    {
        public static AWSCredentials GetCredentials(IConfiguration configuration)
        {
        var service = new AwsCredentialService(configuration);
        var isProduction = (configuration["Environment"] ?? "Development").Equals("Production", StringComparison.OrdinalIgnoreCase);
        var baseCredentials = service.GetBaseCredentials(isProduction);
        var roleArn = configuration["AWS:RoleArn"];
        if (!string.IsNullOrEmpty(roleArn) && isProduction)
        {
            return new AssumeRoleAWSCredentials(baseCredentials, roleArn, "DocuBotAgentSession");
        }
        return baseCredentials;
        }



        public static RegionEndpoint GetRegion(IConfiguration configuration)
        {
            var region = configuration["AWS:Region"] ?? "eu-central-1";
            return RegionEndpoint.GetBySystemName(region);
        }
    }
}
