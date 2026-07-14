using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Mmu.Mlra.ReferenceBuddy.UnitTests
{
    public class AnalyzerTests
    {
        [Fact]
        public async Task Analyzer_DoesNotReportDiagnostic_ForGenericRequestType()
        {
            const string Source = @"
namespace MediatR
{
    public interface IRequest<TResponse>
    {
    }
}

namespace Sample
{
    using MediatR;

    public class Ping<T> : IRequest<int>
    {
    }
}";

            var diagnostics = await AnalyzeAsync(Source);

            diagnostics.Should().NotContain(d => d.Id == "ROL001");
        }

        [Fact]
        public async Task Analyzer_DoesNotReportDiagnostic_WhenRequestIsUsedOutsideTestsAndHandlers()
        {
            const string Source = @"
namespace MediatR
{
    public interface IRequest<TResponse>
    {
    }
}

namespace Sample
{
    using MediatR;

    public class Ping : IRequest<int>
    {
    }

    public class PingController
    {
        public int Execute(Ping request)
        {
            return 1;
        }
    }
}";

            var diagnostics = await AnalyzeAsync(Source);

            diagnostics.Should().NotContain(d => d.Id == "ROL001");
        }

        [Fact]
        public async Task Analyzer_DoesNotTreatNameOnlyHandler_AsAssociatedHandler()
        {
            const string Source = @"
namespace MediatR
{
    public interface IRequest<TResponse>
    {
    }
}

namespace Sample
{
    using MediatR;

    public class Ping : IRequest<int>
    {
    }

    public class PingHandler
    {
        public int Handle(Ping request)
        {
            return 42;
        }
    }
}";

            var diagnostics = await AnalyzeAsync(Source);

            diagnostics.Should().NotContain(d => d.Id == "ROL001");
        }

        [Fact]
        public async Task Analyzer_ReportsDiagnostic_WhenRequestHasNoUsages()
        {
            const string Source = @"
namespace MediatR
{
    public interface IRequest<TResponse>
    {
    }
}

namespace Sample
{
    using MediatR;

    public class Ping : IRequest<int>
    {
    }
}";

            var diagnostics = await AnalyzeAsync(Source);

            diagnostics.Should().ContainSingle(d => d.Id == "ROL001");
            diagnostics.Single(d => d.Id == "ROL001").GetMessage().Should().Contain("Ping");
        }

        private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(params string[] sources)
        {
            var syntaxTrees = sources
                .Select(src => CSharpSyntaxTree.ParseText(src, new CSharpParseOptions(LanguageVersion.Latest)))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                "SampleApp",
                syntaxTrees,
                CreatePlatformReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            compilation.GetDiagnostics().Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new Analyzer());
            var withAnalyzers = compilation.WithAnalyzers(analyzers);

            return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        }

        private static MetadataReference[] CreatePlatformReferences()
        {
            return
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
            ];
        }
    }
}
