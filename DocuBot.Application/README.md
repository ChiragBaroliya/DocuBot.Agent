# DocuBot.Application

**Purpose:**
Defines application-level interfaces and use cases for DocuBot.

## Main Interfaces
- `IGitService`: Abstraction for git operations (diff, branch, validation)

## Example
```csharp
string GetStagedDiff();
string GetCurrentBranch();
bool ValidateBranch(string branchName);
bool ValidateCommit(string commitMessage);
```
