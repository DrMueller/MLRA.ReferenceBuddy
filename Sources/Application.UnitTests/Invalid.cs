using Mmu.Mlra.ReferenceBuddy.Models;
using Mmu.Mlra.ReferenceBuddy.UnitTests.Helpers;
using Xunit;
using VerifyCS = Mmu.Mlra.ReferenceBuddy.UnitTests.Verifiers.CSharpAnalyzerVerifier<Mmu.Mlra.ReferenceBuddy.Analyzer>;

namespace Mmu.Mlra.ReferenceBuddy.UnitTests
{
    public class Invalid
    {
        [Theory]
        [InlineData("Helloworld", "System", "Helloworld Test")]
        public async Task Invalid_Reference(string sourceNamespace, string usingTarget, string ruleFileLine)
        {
            var testCode = UsingTestFactory.Create(sourceNamespace, usingTarget);

            var expectedAnalyzerFailure = VerifyCS.Diagnostic(Diagnostics.RefNotAllowedId)
                .WithSpan(4, 5, 4, 18)
                .WithArguments(sourceNamespace, usingTarget);

            await VerifyCS.VerifyAnalyzerAsync(testCode, ruleFileLine, expectedAnalyzerFailure);
        }
    }
}