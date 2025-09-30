using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Mmu.Mlra.ReferenceBuddy.Models;
using Mmu.Mlra.ReferenceBuddy.Services;
using Mmu.Mlra.ReferenceBuddy.Services.NodeAnalyzers;

namespace Mmu.Mlra.ReferenceBuddy
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.All;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var rulesFile = RulesFileFactory.TryCreating(compilationContext.Options);

                if (rulesFile == null)
                {
                    compilationContext.RegisterCompilationEndAction(compilationEndContext =>
                    {
                        var diagnostic = Diagnostic.Create(Diagnostics.NoRulesFileFoundDiagnostic, null);
                        compilationEndContext.ReportDiagnostic(diagnostic);
                    });

                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(nodeAnalysisContext => { UsingNodeAnalyzer.Analyze(nodeAnalysisContext, rulesFile); }, SyntaxKind.UsingDirective);

                compilationContext.RegisterSyntaxNodeAction(nodeAnalysisContext => { QualifiedNameNodeAnalyzer.Analyze(nodeAnalysisContext, rulesFile); }, SyntaxKind.QualifiedName);

                compilationContext.RegisterSyntaxNodeAction(nodeAnalysisContext => { InvocationExpressionNodeAnalyzer.Analyze(nodeAnalysisContext, rulesFile); }, SyntaxKind.InvocationExpression);
            });
        }
    }
}