using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Zerberuz.Analyzers;

namespace Zerberuz.Benchmarks;

[MemoryDiagnoser]
public sealed class AnalyzerExecutionBenchmark
{
    private static readonly MetadataReference[] References = AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
        .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
        .ToArray();

    private static readonly AnalyzerOptions Options = new(
        ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText(".zerberuz/rules-cache.json", """
            {
              "schemaVersion": "1.0",
              "rulesVersion": "2026.08.11",
              "profile": "benchmark",
              "rules": [
                {
                  "id": "ZBZ001",
                  "type": "naming",
                  "title": "Interfaces must start with I",
                  "severity": "warning",
                  "target": {
                    "symbolKind": "interface"
                  },
                  "condition": {
                    "mustStartWith": "I",
                    "mustMatch": "^I[A-Z].*"
                  },
                  "message": "Interface '{symbolName}' must start with 'I'."
                },
                {
                  "id": "ZBZ100",
                  "type": "folderStructure",
                  "title": "Services must live in a Services folder",
                  "severity": "warning",
                  "target": {
                    "symbolKind": "class",
                    "nameMustMatch": ".*Service$"
                  },
                  "condition": {
                    "pathMustMatch": "src/**/Services/**"
                  },
                  "message": "Service class '{symbolName}' must be placed under a Services folder."
                }
              ],
              "help": [
                {
                  "diagnosticId": "ZBZ001",
                  "title": "Interfaces must start with I"
                },
                {
                  "diagnosticId": "ZBZ100",
                  "title": "Services must live in a Services folder"
                }
              ]
            }
            """)));

    private CSharpCompilation compilation = null!;

    [Params(100, 1000)]
    public int TypeCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var source = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, TypeCount).Select(index =>
                index % 2 == 0
                    ? $"public interface Repository{index} {{ }}"
                    : $"public sealed class Order{index}Service {{ }}"));

        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "src/Orders/OrderServices.cs");
        compilation = CSharpCompilation.Create(
            "Zerberuz.BenchmarkAssembly",
            new[] { syntaxTree },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Benchmark]
    public async Task<int> RunAnalyzer()
    {
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ZerberuzAnalyzer()), Options)
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);

        return diagnostics.Length;
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            this.text = SourceText.From(text);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return text;
        }
    }
}
