# DocuBot.Domain

**Purpose:**
Defines core business rules, entities, and validation logic for DocuBot.

## Main Interfaces
- `IGitValidator`: Validates branch names and commit messages
- `IJiraService`, `IGitHubService`, `ISecretScanner`: Extensibility points for integrations

## Key Classes
- `GitValidator`: Implements branch and commit validation

## Example
```csharp
public bool ValidateBranchName(string branchName);
public bool ValidateCommitMessage(string commitMessage);
```
