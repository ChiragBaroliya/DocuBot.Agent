# DocuBot.AI

**Purpose:**
Provides AI integration for commit message generation and PR description summarization.

## Main Interfaces
- `IOpenAIService`: AI commit/PR generation
- `IConventionalCommitGenerator`: Strict Conventional Commit formatting

## Key Classes
- `OpenAIService`: Integrates with OpenAI API
- `ConventionalCommitGenerator`: Enforces commit message rules

## Example
```csharp
Task<string> GenerateCommitMessageAsync(string diff);
Task<string> GeneratePRDescriptionAsync(string diff);
```
