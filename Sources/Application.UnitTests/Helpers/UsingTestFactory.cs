namespace Mmu.Mlra.ReferenceBuddy.UnitTests.Helpers
{
    internal static class UsingTestFactory
    {
        private const string UsingTemplate = @"
namespace PLACEHOLDER1
{
    using PLACEHOLDER2;

    public class TestClass
    {
    }
}";

        internal static string Create(string sourceNamespace, string usingTarget)
        {
            var str = UsingTemplate.Replace("PLACEHOLDER1", sourceNamespace);

            return str.Replace("PLACEHOLDER2", usingTarget);
        }
    }
}