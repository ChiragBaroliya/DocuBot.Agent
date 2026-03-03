# DocuBot.CLI

**Purpose:**
Provides a command-line interface for DocuBot, suitable for use in git hooks and automation.

## Main Commands
- `validate`: Validates branch naming
- `suggest-commit`: Suggests AI commit message
- `pre-push`: Blocks push if validation fails

## Example Usage
```sh
dotnet run --project DocuBot.CLI validate
dotnet run --project DocuBot.CLI suggest-commit
dotnet run --project DocuBot.CLI pre-push
```
