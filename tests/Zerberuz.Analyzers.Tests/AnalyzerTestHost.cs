using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Zerberuz.Analyzers.Tests;

public sealed class AnalyzerTestHost
{
    public async Task<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        string filePath = "Test.cs",
        IReadOnlyCollection<AdditionalText>? additionalFiles = null,
        CancellationToken cancellationToken = default)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath, cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Zerberuz.AnalyzerTests",
            syntaxTrees: new[] { syntaxTree },
            references: CreateMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(analyzer),
            new AnalyzerOptions((additionalFiles ?? Array.Empty<AdditionalText>()).ToImmutableArray()));

        var diagnostics = await compilationWithAnalyzers
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        return diagnostics
            .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToArray();
    }

    private static IReadOnlyCollection<MetadataReference> CreateMetadataReferences()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .ToArray();
    }
}
