using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DocuBot.AI.Interfaces;
using DocuBot.Application.Interfaces;

namespace DocuBot.AI.Services
{
    public class FunctionalDocGenerator : IFunctionalDocGenerator
    {
        private readonly IAiModelService _aiModelService;

        public FunctionalDocGenerator(IAiModelService aiModelService)
        {
            _aiModelService = aiModelService;
        }

        public async Task<string> GenerateForDiffAsync(string diff)
        {
            // For simplicity, treat diff as C# code blocks (new/changed code)
            var sb = new StringBuilder();
            sb.AppendLine("# Functional Documentation for Diff\n");

            var tree = CSharpSyntaxTree.ParseText(diff);
            var root = await tree.GetRootAsync();

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

            foreach (var classNode in classes)
            {
                var className = classNode.Identifier.Text;
                var classSummary = GetXmlSummary(classNode);
                if (string.IsNullOrWhiteSpace(classSummary))
                {
                    // Use AI to generate summary if XML comment is missing
                    classSummary = await _aiModelService.GenerateDocumentationAsync(classNode.NormalizeWhitespace().ToFullString());
                }
                sb.AppendLine($"## {className}\n{classSummary}\n");

                var methods = classNode.Members.OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));

                foreach (var method in methods)
                {
                    var methodName = method.Identifier.Text;
                    var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                    var methodSummary = GetXmlSummary(method);
                    if (string.IsNullOrWhiteSpace(methodSummary))
                    {
                        methodSummary = await _aiModelService.GenerateDocumentationAsync(method.NormalizeWhitespace().ToFullString());
                    }
                    sb.AppendLine($"- **{methodName}({parameters})**: {methodSummary}\n");
                }
            }

            return sb.ToString();
        }

        public async Task<string> GenerateForCodebaseAsync(string solutionPath)
        {
            var csFiles = Directory.GetFiles(solutionPath, "*.cs", SearchOption.AllDirectories);
            var sb = new StringBuilder();
            sb.AppendLine("# Functional Documentation\n");

            foreach (var file in csFiles)
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

                foreach (var classNode in classes)
                {
                    var className = classNode.Identifier.Text;
                    var classSummary = GetXmlSummary(classNode);
                    if (string.IsNullOrWhiteSpace(classSummary))
                    {
                        classSummary = await _aiModelService.GenerateDocumentationAsync(classNode.NormalizeWhitespace().ToFullString());
                    }
                    sb.AppendLine($"## {className}\n{classSummary}\n");

                    var methods = classNode.Members.OfType<MethodDeclarationSyntax>()
                        .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));

                    foreach (var method in methods)
                    {
                        var methodName = method.Identifier.Text;
                        var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                        var methodSummary = GetXmlSummary(method);
                        if (string.IsNullOrWhiteSpace(methodSummary))
                        {
                            methodSummary = await _aiModelService.GenerateDocumentationAsync(method.NormalizeWhitespace().ToFullString());
                        }
                        sb.AppendLine($"- **{methodName}({parameters})**: {methodSummary}\n");
                    }
                }
            }

            return sb.ToString();
        }

        private string GetXmlSummary(MemberDeclarationSyntax member)
        {
            var trivia = member.GetLeadingTrivia();
            var xmlComment = trivia.Select(x => x.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();
            if (xmlComment != null)
            {
                var summary = xmlComment.Content.OfType<XmlElementSyntax>()
                    .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "summary");
                if (summary != null)
                {
                    return string.Join(" ", summary.Content.Select(c => c.ToString()).ToArray()).Trim();
                }
            }
            return string.Empty;
        }
    }
}
