using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Zerberuz.Analyzers.Tests;

public sealed class AnalyzerTestHostTests
{
    [Fact]
    public async Task GetDiagnosticsAsync_returns_analyzer_diagnostics()
    {
        var diagnostics = await new AnalyzerTestHost().GetDiagnosticsAsync(
            "public sealed class Customer { }",
            new TestAnalyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ZBZTST001", diagnostic.Id);
    }

    private sealed class TestAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Descriptor = new(
            "ZBZTST001",
            "Test diagnostic",
            "Test diagnostic",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxTreeAction(analysisContext =>
            {
                var location = analysisContext.Tree.GetRoot(analysisContext.CancellationToken).GetLocation();
                analysisContext.ReportDiagnostic(Diagnostic.Create(Descriptor, location));
            });
        }
    }
}
