# DocuBot.MCP

**Purpose:**
Implements the MCP server exposing tool-calling APIs for git operations and validation.

## Main Features
- REST API endpoints for:
  - Get staged diff
  - Get current branch
  - Validate branch
  - Validate commit
- Returns JSON structured responses

## Example Endpoints
```
GET /api/tools/get_staged_diff
GET /api/tools/get_current_branch
POST /api/tools/validate_branch
POST /api/tools/validate_commit
```
