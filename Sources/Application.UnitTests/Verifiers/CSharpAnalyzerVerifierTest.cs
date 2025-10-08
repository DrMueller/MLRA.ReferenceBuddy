using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Mmu.Mlra.ReferenceBuddy.Services;

namespace Mmu.Mlra.ReferenceBuddy.UnitTests.Verifiers
{
    public static partial class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            public Test(string ruleFuleLine)
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId)?.CompilationOptions;

                    if (compilationOptions == null)
                    {
                        return solution;
                    }

                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    solution = solution.AddAdditionalDocument(
                        DocumentId.CreateNewId(projectId),
                        RulesFileFactory.FileName,
                        SourceText.From(ruleFuleLine, Encoding.UTF8));

                    return solution;
                });
            }
        }
    }
}