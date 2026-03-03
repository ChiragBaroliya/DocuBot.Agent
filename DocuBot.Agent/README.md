# DocuBot.Agent

**Purpose:**
Entry point for DocuBot. Enforces Git branch and commit standards, blocks push if rules are broken, and provides AI-assisted commit message suggestions.

## Main Features
- Validates branch naming convention (`feature/*`, `bugfix/*`, `hotfix/*`)
- Validates Conventional Commit message format
- Blocks push if validation fails
- Reads staged git changes
- Suggests AI-generated Conventional Commit message

## Entry Point
See `Program.cs` for main logic:
```csharp
// Validates branch and commit, blocks push if invalid, suggests AI commit message
```

## Usage
```sh
dotnet run --project DocuBot.Agent
```
