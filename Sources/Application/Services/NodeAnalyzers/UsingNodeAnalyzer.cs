using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Mmu.Mlra.ReferenceBuddy.Models;

namespace Mmu.Mlra.ReferenceBuddy.Services.NodeAnalyzers
{
    internal static class UsingNodeAnalyzer
    {
        internal static void Analyze(
            SyntaxNodeAnalysisContext analysisContext,
            RulesFile rulesFile)
        {
            if (!(analysisContext.Node is UsingDirectiveSyntax usingDirectiveSyntax))
            {
                return;
            }

            // ReSharper disable once PossibleNullReferenceException
            var targetName = usingDirectiveSyntax.Name.ToFullString().Trim();
            var stripClass = false;
            var isGlobal = usingDirectiveSyntax.GlobalKeyword.Text == "global";

            if (usingDirectiveSyntax.ChildTokens().Any(n => n.IsKind(SyntaxKind.StaticKeyword)))
            {
                stripClass = true;
            }

            else if (usingDirectiveSyntax.ChildNodes().OfType<NameEqualsSyntax>().Any())
            {
                if (analysisContext
                        .SemanticModel
                        .GetTypeInfo(usingDirectiveSyntax.ChildNodes().OfType<QualifiedNameSyntax>().First())
                        .Type != null)
                {
                    stripClass = true;
                }
            }

            if (stripClass)
            {
                targetName = usingDirectiveSyntax.ChildNodes().OfType<QualifiedNameSyntax>().FirstOrDefault()?.Left
                                 .ToFullString().Trim()
                             ?? targetName;
            }

            var sourceName = RoslynHelper.FindContainingNamespace(usingDirectiveSyntax)?.Name.ToFullString().Trim();

            if (isGlobal)
            {
                rulesFile.AnalyzeReference(analysisContext, usingDirectiveSyntax.GetLocation(), "*", targetName);

                return;
            }

            if (sourceName == null)
            {
                var namespaces = usingDirectiveSyntax.Parent?.DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>();

                if (namespaces != null)
                {
                    foreach (var ns in namespaces)
                    {
                        sourceName = ns.Name.ToFullString().Trim();
                        rulesFile.AnalyzeReference(analysisContext, usingDirectiveSyntax.GetLocation(), sourceName,
                            targetName);
                    }
                }
            }

            rulesFile.AnalyzeReference(analysisContext, usingDirectiveSyntax.GetLocation(), sourceName, targetName);
        }
    }
}