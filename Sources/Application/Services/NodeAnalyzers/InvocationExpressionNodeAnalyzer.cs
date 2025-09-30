using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Mmu.Mlra.ReferenceBuddy.Models;

namespace Mmu.Mlra.ReferenceBuddy.Services.NodeAnalyzers
{
    internal static class InvocationExpressionNodeAnalyzer
    {
        internal static void Analyze(
            SyntaxNodeAnalysisContext analysisContext,
            RulesFile rulesFile)
        {
            var expressionSyntax = (InvocationExpressionSyntax)analysisContext.Node;

            var idNames = expressionSyntax.ChildNodes().OfType<IdentifierNameSyntax>().ToList();

            if (!idNames.Any()
                || idNames.All(n => n.Identifier.ValueText != "nameof"))
            {
                return;
            }

            foreach (var expression in expressionSyntax
                         .ChildNodes()
                         .OfType<ArgumentListSyntax>()
                         .SelectMany(a => a.Arguments.Select(b => b.Expression)))
            {
                string targetName;
                var symbol = analysisContext.SemanticModel.GetTypeInfo(expression).Type
                             ?? analysisContext.SemanticModel.GetSymbolInfo(expression).Symbol;

                if (symbol == null)
                {
                    return;
                }

                switch (symbol.Kind)
                {
                    case SymbolKind.NamedType:
                        targetName = symbol.ContainingNamespace.ToDisplayString();

                        break;

                    case SymbolKind.Namespace when !((symbol as INamespaceSymbol)?.IsGlobalNamespace ?? false):
                        targetName = symbol.ToDisplayString();

                        break;

                    default:
                        return;
                }

                var containingNamespace = RoslynHelper.FindContainingNamespace(expressionSyntax);

                if (containingNamespace == null)
                {
                    return;
                }

                var sourceName = containingNamespace.Name.ToFullString().Trim();
                rulesFile.AnalyzeReference(analysisContext, expressionSyntax.GetLocation(), sourceName, targetName);
            }
        }
    }
}