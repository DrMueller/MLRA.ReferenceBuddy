using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mmu.Mlra.ReferenceBuddy.Services
{
    internal static class RoslynHelper
    {
        internal static BaseNamespaceDeclarationSyntax FindContainingNamespace(CSharpSyntaxNode syntaxNode)
        {
            BaseNamespaceDeclarationSyntax result = syntaxNode.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            if (result == null)
            {
                result = syntaxNode
                    .FirstAncestorOrSelf<CompilationUnitSyntax>()?.ChildNodes()
                    .OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
            }

            return result;
        }
    }
}