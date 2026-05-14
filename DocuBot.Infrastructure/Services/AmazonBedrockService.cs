using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using DocuBot.Application.Interfaces;

namespace DocuBot.Infrastructure.Services
{
    public class AmazonBedrockService : IAiModelService
    {
        private readonly IAmazonBedrockRuntime _client;
        private readonly string _defaultModelId;

        public AmazonBedrockService()
        {
            // Defaulting to the model specified by the user
            _defaultModelId = Environment.GetEnvironmentVariable("AWS_BEDROCK_MODEL_ID") ?? "meta.llama3-3-70b-instruct-v1:0";
            
            var credentials = AwsCredentialProvider.GetCredentials();
            var region = AwsCredentialProvider.GetRegion();
            
            _client = credentials != null 
                ? new AmazonBedrockRuntimeClient(credentials, region)
                : new AmazonBedrockRuntimeClient(region);
        }

        public AmazonBedrockService(IAmazonBedrockRuntime client)
        {
            _client = client;
            _defaultModelId = Environment.GetEnvironmentVariable("AWS_BEDROCK_MODEL_ID") ?? "meta.llama3-3-70b-instruct-v1:0";
        }

        public async Task<string> GetResponseAsync(string model, string input)
        {
            // Llama 3 prompt format for Bedrock
            // Note: For better results with Llama 3, you might want to wrap the prompt in Llama 3 chat tags:
            // <|begin_of_text|><|start_header_id|>user<|end_header_id|>\n\n{input}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n
            
            var payload = new
            {
                prompt = input,
                max_gen_len = 2048,
                temperature = 0.5,
                top_p = 0.9
            };

            var requestJson = JsonSerializer.Serialize(payload);
            
            var request = new InvokeModelRequest
            {
                ModelId = model,
                Body = new MemoryStream(Encoding.UTF8.GetBytes(requestJson)),
                ContentType = "application/json"
            };

            try
            {
                var response = await _client.InvokeModelAsync(request);
                
                using (var reader = new StreamReader(response.Body))
                {
                    var responseJson = await reader.ReadToEndAsync();
                    return ExtractTextFromResponse(responseJson);
                }
            }
            catch (Exception ex)
            {
                return $"[AmazonBedrockService Error] {ex.Message}";
            }
        }

        public async Task<string> GenerateCommitMessageAsync(string diff)
        {
            string guidance =
                "Use Conventional Commits format:\n" +
                "feat: A new feature\n" +
                "fix: A bug fix\n" +
                "docs: Documentation only\n" +
                "style: Formatting only\n" +
                "refactor: Code restructuring\n" +
                "perf: Performance improvement\n" +
                "test: Adding/updating tests\n" +
                "chore: Maintenance tasks\n\n" +
                "IMPORTANT:\n" +
                "- Return a detailed multi-line commit message\n" +
                "- Start with a subject line (type: short description)\n" +
                "- Followed by a blank line and then a detailed body explaining the changes\n" +
                "- Use the body to explain 'what' and 'why', not just 'how'\n" +
                "- Do NOT include conversational filler or prefix like 'Here is the message'\n\n";

            string prompt = $"{guidance}Git diff:\n{diff}";
            return await GetResponseAsync(_defaultModelId, prompt);
        }

        public async Task<bool> ValidateCommitMessageAsync(string commitMessage, string diff)
        {
            string guidance =
                "You are a strict git commit reviewer. Analyze the provided git diff and the given commit message.\n" +
                "Does the commit message accurately describe the changes in the git diff?\n" +
                "Return EXACTLY 'true' if the message is accurate and appropriate for the changes.\n" +
                "Return EXACTLY 'false' if the message is incorrect, misleading, or completely unrelated.\n" +
                "IMPORTANT: Return ONLY the word 'true' or 'false', nothing else.";

            string prompt = $"{guidance}\n\nCommit Message:\n{commitMessage}\n\nGit Diff:\n{diff}";

            var responseText = (await GetResponseAsync(_defaultModelId, prompt)).Trim().ToLower();
            return responseText.Contains("true");
        }

        public async Task<string> GeneratePRDescriptionAsync(string diff)
        {
            string prompt = $"Write a detailed pull request description for the following code changes:\n{diff}";
            return await GetResponseAsync(_defaultModelId, prompt);
        }

        public async Task<string> GenerateDocumentationAsync(string codeOrComments)
        {
            string prompt = $@"
You are a senior software analyst.
Given the following C# code, generate a clear, business-oriented functional documentation section for it.

For a class, include:
- Feature Name
- Purpose (what business or user problem does it solve)
- Actors (who uses it)
- Preconditions (what must be true before using it)
- User Flow (step-by-step, what happens)
- Business Rules (any constraints or logic)
- Validations (input checks)
- Error Handling (how errors are handled)
- Notifications (emails, messages, etc.)
- Admin Features (if any)
- Expected Output (what is produced or changed)

For a method, include:
- Purpose
- Parameters and their meaning
- Preconditions
- Step-by-step logic
- Business rules
- Validations
- Error handling
- Output

Respond in Markdown format.
Here is the code:
{codeOrComments}
";
            return await GetResponseAsync(_defaultModelId, prompt);
        }

        public async Task<string> GenerateCodeReviewAsync(string diff)
        {
            string guidance = 
                "You are an expert security code reviewer focusing on OWASP Top 10 security risks.\n" +
                "Review the provided git diff for staged files and provide suggestions ONLY for HIGH and CRITICAL severity issues related to OWASP Top 10 (e.g., SQL injection, Sensitive Data Exposure, etc.).\n" +
                "If there are no HIGH or CRITICAL issues, respond with 'Status: PASS - No high or critical issues found.'\n" +
                "Otherwise, respond with 'Status: REVIEW_REQUIRED' followed by a detailed markdown list of the violations including the specific OWASP category.\n" +
                "IMPORTANT: Your response MUST contain either 'Status: PASS' or 'Status: REVIEW_REQUIRED' prominently.\n" +
                "Format the output as a Markdown report.\n";

            string prompt = $"{guidance}\n\nGit Diff:\n{diff}";
            return await GetResponseAsync(_defaultModelId, prompt);
        }

        private string ExtractTextFromResponse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                
                // Bedrock Llama 3 response format
                if (doc.RootElement.TryGetProperty("generation", out var generationProp))
                {
                    return generationProp.GetString() ?? string.Empty;
                }
                
                // Generic fallback for other models or if format changes
                return responseJson;
            }
            catch (Exception ex)
            {
                return $"[AI Response Parsing Error]: {ex.Message}\nRaw response: {responseJson}";
            }
        }
    }
}
