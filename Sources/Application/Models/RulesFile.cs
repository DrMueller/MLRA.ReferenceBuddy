using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mmu.Mlra.ReferenceBuddy.Models
{
    internal class RulesFile
    {
        private readonly IReadOnlyCollection<ReferenceRule> _rules;

        public RulesFile(IReadOnlyCollection<ReferenceRule> rules)
        {
            _rules = rules;
        }

        internal void AnalyzeReference(
            SyntaxNodeAnalysisContext analysisContext,
            Location location,
            string sourceName,
            string targetName)
        {
            if (CheckIsAllowedReference(sourceName, targetName))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(
                Diagnostics.ReferenceNotAllowedDiagnostic,
                location,
                sourceName,
                targetName);

            analysisContext.ReportDiagnostic(diagnostic);
        }

        private bool CheckIsAllowedReference(string sourceName, string targetName)
        {
            const string global = "global::";

            if (sourceName.StartsWith(global))
            {
                sourceName = sourceName.Substring(global.Length);
            }

            if (targetName.StartsWith(global))
            {
                targetName = targetName.Substring(global.Length);
            }

            if (sourceName == targetName)
            {
                return true;
            }

            return _rules.Any(f => f.IsMatch(sourceName, targetName));
        }
    }
}