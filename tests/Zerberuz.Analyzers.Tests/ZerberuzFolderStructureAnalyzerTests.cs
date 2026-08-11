using Zerberuz.Analyzers;

namespace Zerberuz.Analyzers.Tests;

public sealed class ZerberuzFolderStructureAnalyzerTests
{
    [Fact]
    public async Task Analyzer_reports_ZBZ100_for_service_class_outside_services_folder()
    {
        var diagnostics = await new AnalyzerTestHost().GetDiagnosticsAsync(
            "public sealed class OrderService { }",
            new ZerberuzAnalyzer(),
            filePath: "src/Orders/OrderService.cs",
            additionalFiles: new[]
            {
                new InMemoryAdditionalText(".zerberuz/rules-cache.json", RuleCache)
            });

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ZBZ100", diagnostic.Id);
    }

    [Fact]
    public async Task Analyzer_does_not_report_ZBZ100_for_service_class_inside_services_folder()
    {
        var diagnostics = await new AnalyzerTestHost().GetDiagnosticsAsync(
            "public sealed class OrderService { }",
            new ZerberuzAnalyzer(),
            filePath: "src/Orders/Services/OrderService.cs",
            additionalFiles: new[]
            {
                new InMemoryAdditionalText(".zerberuz/rules-cache.json", RuleCache)
            });

        Assert.Empty(diagnostics);
    }

    private const string RuleCache = """
    {
      "schemaVersion": "1.0",
      "rulesVersion": "2026.08.11",
      "profile": "backend",
      "rules": [
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
          "diagnosticId": "ZBZ100",
          "title": "Services must live in a Services folder"
        }
      ]
    }
    """;
}
