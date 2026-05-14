using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

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

        public AwsCredentialService()
        {
            _environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development";
            _roleArn = Environment.GetEnvironmentVariable("AWS_ROLE_ARN");
            _profileName = Environment.GetEnvironmentVariable("AWS_PROFILE");
            
            var regionName = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-central-1";
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

        private AWSCredentials GetBaseCredentials(bool isProduction)
        {
            if (isProduction)
            {
                return FallbackCredentialsFactory.GetCredentials();
            }

            // 1. Try explicitly provided keys in .env
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                if (!string.IsNullOrEmpty(sessionToken))
                    return new SessionAWSCredentials(accessKey, secretKey, sessionToken);
                
                return new BasicAWSCredentials(accessKey, secretKey);
            }

            // 2. Try AWS Profile (SSO/Local)
            if (!string.IsNullOrEmpty(_profileName))
            {
                var chain = new CredentialProfileStoreChain();
                if (chain.TryGetAWSCredentials(_profileName, out var profileCredentials))
                {
                    return profileCredentials;
                }
            }

            // 3. Fallback to default chain
            return FallbackCredentialsFactory.GetCredentials();
        }
    }

    /// <summary>
    /// Legacy static provider updated to use the new service.
    /// This ensures backward compatibility with existing code.
    /// </summary>
    public static class AwsCredentialProvider
    {
        public static AWSCredentials GetCredentials()
        {
            // For sync calls, we use a specialized version or run async task synchronously
            // However, the best practice is to use AssumeRoleAWSCredentials for automatic refreshing
            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development";
            var roleArn = Environment.GetEnvironmentVariable("AWS_ROLE_ARN");
            
            var service = new AwsCredentialService();
            var baseCredentials = (service as dynamic).GetBaseCredentials(environment.Equals("Production", StringComparison.OrdinalIgnoreCase)) as AWSCredentials;

            if (!string.IsNullOrEmpty(roleArn))
            {
                // Use the SDK's automatic refreshing AssumeRole credentials
                return new AssumeRoleAWSCredentials(baseCredentials, roleArn, "DocuBotAgentSession");
            }

            return baseCredentials;
        }

        public static RegionEndpoint GetRegion()
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-central-1";
            return RegionEndpoint.GetBySystemName(region);
        }
    }
}
