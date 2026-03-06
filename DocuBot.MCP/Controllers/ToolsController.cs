using Microsoft.AspNetCore.Mvc;
using DocuBot.MCP.Services;
using DocuBot.MCP.Models;

namespace DocuBot.MCP.Controllers
{
    [ApiController]
    [Route("api/tools")]
    public class ToolsController : ControllerBase
    {
        private readonly IGitService _gitService;

        public ToolsController(IGitService gitService)
        {
            _gitService = gitService;
        }

        [HttpGet("get_staged_diff")]
        public ActionResult<McpToolResponse> GetStagedDiff()
        {
            var diff = _gitService.GetStagedDiff();
            return Ok(new McpToolResponse { Success = true, Data = diff });
        }

        [HttpGet("get_current_branch")]
        public ActionResult<McpToolResponse> GetCurrentBranch()
        {
            var branch = _gitService.GetCurrentBranch();
            return Ok(new McpToolResponse { Success = true, Data = branch });
        }

        [HttpPost("validate_branch")]
        public ActionResult<McpToolResponse> ValidateBranch([FromBody] string branchName)
        {
            var valid = _gitService.ValidateBranch(branchName);
            return Ok(new McpToolResponse { Success = valid, Message = valid ? "Valid" : "Invalid" });
        }

        [HttpPost("validate_commit")]
        public ActionResult<McpToolResponse> ValidateCommit([FromBody] string commitMessage)
        {
            var valid = _gitService.ValidateCommit(commitMessage);
            return Ok(new McpToolResponse { Success = valid, Message = valid ? "Valid" : "Invalid" });
        }
    }
}
