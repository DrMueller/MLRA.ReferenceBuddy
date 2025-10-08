using System.Text.RegularExpressions;

namespace Mmu.Mlra.ReferenceBuddy.Models
{
    public class ReferenceRule
    {
        private readonly bool _invert;
        private readonly Regex _sourceRegex;
        private readonly Regex _targetRegex;

        public ReferenceRule(string sourcePattern, string targetPattern, bool invert)
        {
            _sourceRegex = ToRegex(sourcePattern);
            _targetRegex = ToRegex(targetPattern);
            _invert = invert;
        }

        public bool IsMatch(string sourceName, string targetName)
        {
            return _invert ^ (_sourceRegex.IsMatch(sourceName) && _targetRegex.IsMatch(targetName));
        }

        private static Regex ToRegex(string str)
        {
            var pattern = "^" + Regex.Escape(str)
                .Replace(@"\*", ".*") + "$";

            return new Regex(pattern, RegexOptions.Compiled);
        }
    }
}