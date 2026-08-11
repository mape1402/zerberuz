using Zerberuz.Analyzers;

namespace Zerberuz.Analyzers.Tests;

public sealed class ZerberuzGeneratedCodeTests
{
    [Theory]
    [InlineData("obj/Debug/net8.0/GeneratedClient.g.cs")]
    [InlineData("obj/Debug/net8.0/GeneratedClient.generated.cs")]
    [InlineData("obj/Debug/net8.0/GeneratedClient.Designer.cs")]
    public async Task Analyzer_ignores_generated_file_paths(string filePath)
    {
        var diagnostics = await new AnalyzerTestHost().GetDiagnosticsAsync(
            "public interface Repository { } public sealed class OrderService { }",
            new ZerberuzAnalyzer(),
            filePath: filePath,
            additionalFiles: new[]
            {
                new InMemoryAdditionalText(".zerberuz/rules-cache.json", """
                {
                  "schemaVersion": "1.0",
                  "rulesVersion": "2026.08.11",
                  "profile": "backend",
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
                        "mustStartWith": "I"
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
                """)
            });

        Assert.Empty(diagnostics);
    }
}
