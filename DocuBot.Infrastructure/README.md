# DocuBot.Infrastructure

**Purpose:**
Implements application and domain interfaces, provides integration with external systems (Git, etc).

## Main Classes
- `GitExecutor`: Executes git commands and validates using domain logic

## Example
```csharp
public string GetStagedDiff();
public string GetCurrentBranch();
```
