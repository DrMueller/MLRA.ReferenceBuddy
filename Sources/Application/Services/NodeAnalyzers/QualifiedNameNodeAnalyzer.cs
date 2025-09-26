using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Mmu.Mlra.ReferenceBuddy.Models;

namespace Mmu.Mlra.ReferenceBuddy.Services.NodeAnalyzers
{
    internal static class QualifiedNameNodeAnalyzer
    {
        internal static void Analyze(
            SyntaxNodeAnalysisContext analysisContext,
            RulesFile rulesFile)
        {
            var qualifiedNameSyntax = analysisContext.Node as QualifiedNameSyntax;
            if (qualifiedNameSyntax == null)
            {
                return;
            }

            switch (qualifiedNameSyntax.Parent?.Kind())
            {
                case SyntaxKind.QualifiedName:
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.UsingDirective:
                    return;
            }

            string targetName;
            var symbol = analysisContext.SemanticModel.GetTypeInfo(qualifiedNameSyntax).Type
                         ?? analysisContext.SemanticModel.GetSymbolInfo(qualifiedNameSyntax).Symbol;

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

            var containingNamespace = RoslynHelper.FindContainingNamespace(qualifiedNameSyntax);
            if (containingNamespace == null)
            {
                return;
            }

            var sourceName = containingNamespace.Name.ToFullString().Trim();
            rulesFile.AnalyzeReference(analysisContext, qualifiedNameSyntax.GetLocation(), sourceName, targetName);
        }
    }
}