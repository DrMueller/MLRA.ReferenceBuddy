using Mmu.Mlra.ReferenceBuddy.UnitTests.Helpers;
using Xunit;
using VerifyCS = Mmu.Mlra.ReferenceBuddy.UnitTests.Verifiers.CSharpAnalyzerVerifier<Mmu.Mlra.ReferenceBuddy.Analyzer>;

namespace Mmu.Mlra.ReferenceBuddy.UnitTests
{
    public class Valid
    {
        [Theory]
        [InlineData("Helloworld", "System", "Helloworld System")]
        public async Task Valid_Reference(string sourceNamespace, string usingTarget, string ruleFileLine)
        {
            var testCode = UsingTestFactory.Create(sourceNamespace, usingTarget);
            await VerifyCS.VerifyAnalyzerAsync(testCode, ruleFileLine);
        }
    }
}