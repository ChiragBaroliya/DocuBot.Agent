using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;

namespace DocuBot.Infrastructure.Services
{
    public static class AwsCredentialProvider
    {
        public static AWSCredentials GetCredentials()
        {
            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development";
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
            var roleArn = Environment.GetEnvironmentVariable("AWS_ROLE_ARN");

            AWSCredentials credentials;

            // In Production, we prioritize the Role-based credentials (IAM Instance Profile, etc.)
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                // Fallback to default credential chain which picks up Role-based identity automatically in AWS environments
                credentials = FallbackCredentialsFactory.GetCredentials();
            }
            else
            {
                // In Local/Development, we prioritize .env file credentials if present
                if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
                {
                    if (!string.IsNullOrEmpty(sessionToken))
                    {
                        credentials = new SessionAWSCredentials(accessKey, secretKey, sessionToken);
                    }
                    else
                    {
                        credentials = new BasicAWSCredentials(accessKey, secretKey);
                    }
                }
                else
                {
                    // Fallback to default credential chain if keys are not provided
                    credentials = FallbackCredentialsFactory.GetCredentials();
                }
            }

            // If a specific Role ARN is provided, we assume that role using the credentials obtained above
            if (!string.IsNullOrEmpty(roleArn))
            {
                // In AWS SDK v4, we can use the credentials to assume a role
                credentials = new AssumeRoleAWSCredentials(credentials, roleArn, "DocuBotAgentSession");
            }

            return credentials;
        }

        public static RegionEndpoint GetRegion()
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            return RegionEndpoint.GetBySystemName(region);
        }
    }
}
