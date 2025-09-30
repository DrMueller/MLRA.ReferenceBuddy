using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Mmu.Mlra.ReferenceBuddy.Common.Types.Maybes;
using Mmu.Mlra.ReferenceBuddy.Common.Types.Maybes.Implementation;
using Mmu.Mlra.ReferenceBuddy.Models;

namespace Mmu.Mlra.ReferenceBuddy.Services
{
    internal static class RulesFileFactory
    {
        public const string FileName = ".refbuddyrules";
        private const string RuleSeparator = " ";
        private static readonly string[] _commentIndicators = { "#", "//" };

        public static RulesFile TryCreating(AnalyzerOptions options)
        {
            var rulesFiles = options
                .AdditionalFiles
                .Where(f => FileName.Equals(Path.GetFileName(f.Path), StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (rulesFiles.Count != 1)
            {
                return null;
            }

            var rulesText = rulesFiles.Single().GetText();

            if (rulesText == null)
            {
                return null;
            }

            var refRules = rulesText
                .Lines.Select(TryMapping)
                .SelectSome()
                .ToList();

            return new RulesFile(refRules);
        }

        private static Maybe<ReferenceRule> TryMapping(TextLine line)
        {
            var lineText = line.ToString();

            if (_commentIndicators.Any(ci => lineText.TrimStart().StartsWith(ci)))
            {
                return None.Value;
            }

            var parts = lineText.Split(new[] { RuleSeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return None.Value;
            }

            var sourcePattern = parts[0].Trim();
            var targetPattern = parts[1].Trim();

            var isInverted = false;

            if (sourcePattern.StartsWith("!"))
            {
                isInverted = true;
                sourcePattern = sourcePattern.Substring(1);
            }

            return new ReferenceRule(sourcePattern, targetPattern, isInverted);
        }
    }
}