using Microsoft.AspNetCore.Mvc;
using DocuBot.Application.Interfaces;
using DocuBot.WebApi.Models;

namespace DocuBot.WebApi.Controllers
{
    [ApiController]
    [Route("api/commit")]
    public class CommitSuggestionController : ControllerBase
    {
        private readonly IAiModelService _aiModelService;
        private readonly ILogger<CommitSuggestionController> _logger;

        public CommitSuggestionController(IAiModelService aiModelService, ILogger<CommitSuggestionController> logger)
        {
            _aiModelService = aiModelService;
            _logger = logger;
        }

        [HttpPost("suggest")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SuggestCommitMessage([FromBody] CommitSuggestionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Diff))
            {
                _logger.LogWarning("Suggestion requested with empty diff payload.");
                return BadRequest("Git diff cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Generating commit message suggestion using AWS Bedrock...");
                string suggestion = await _aiModelService.GenerateCommitMessageAsync(request.Diff);
                
                if (suggestion.StartsWith("[AmazonBedrockService Error]"))
                {
                    _logger.LogError("Error from AmazonBedrockService: {Error}", suggestion);
                    return StatusCode(StatusCodes.Status500InternalServerError, suggestion);
                }

                _logger.LogInformation("Successfully generated commit message suggestion.");
                return Ok(suggestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating commit message suggestion.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost("validate")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ValidateCommitMessage([FromBody] ValidationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Diff) || string.IsNullOrWhiteSpace(request.CommitMessage))
            {
                _logger.LogWarning("Validation requested with empty payload.");
                return BadRequest("Commit message and git diff cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Validating commit message semantically using AWS Bedrock...");
                bool isValid = await _aiModelService.ValidateCommitMessageAsync(request.CommitMessage, request.Diff);
                
                _logger.LogInformation("Successfully completed semantic commit message validation. Result: {IsValid}", isValid);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating commit message.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost("review-report")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateCodeReviewHtmlReport([FromBody] CommitSuggestionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Diff))
            {
                _logger.LogWarning("Code review requested with empty diff payload.");
                return BadRequest("Git diff cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Generating OWASP code review HTML report using AWS Bedrock...");
                string report = await _aiModelService.GenerateCodeReviewHtmlReportAsync(request.Diff);
                
                if (report.StartsWith("[AmazonBedrockService Error]"))
                {
                    _logger.LogError("Error from AmazonBedrockService during code review: {Error}", report);
                    return StatusCode(StatusCodes.Status500InternalServerError, report);
                }

                _logger.LogInformation("Successfully generated code review HTML report.");
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating code review report.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost("master-functional-readme")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateMasterFunctionalReadme([FromBody] ReadmeGenerationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProjectDescription))
            {
                _logger.LogWarning("Functional README requested with empty project description payload.");
                return BadRequest("Project description cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Generating functional README using AI model service...");
                string readme = await _aiModelService.GenerateMasterFunctionalReadmeAsync(request.ProjectDescription);

                if (readme.StartsWith("[AmazonBedrockService Error]"))
                {
                    _logger.LogError("Error from AmazonBedrockService during functional README generation: {Error}", readme);
                    return StatusCode(StatusCodes.Status500InternalServerError, readme);
                }

                _logger.LogInformation("Successfully generated functional README.");
                return Ok(readme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating functional README.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
