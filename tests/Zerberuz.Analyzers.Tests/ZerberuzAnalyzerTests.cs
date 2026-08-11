using Zerberuz.Analyzers;

namespace Zerberuz.Analyzers.Tests;

public sealed class ZerberuzAnalyzerTests
{
    [Fact]
    public async Task Analyzer_reports_ZBZ001_for_interface_that_violates_cached_rule()
    {
        var diagnostics = await new AnalyzerTestHost().GetDiagnosticsAsync(
            "public interface Repository { }",
            new ZerberuzAnalyzer(),
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
                        "mustStartWith": "I",
                        "mustMatch": "^I[A-Z].*"
                      },
                      "message": "Interface '{symbolName}' must start with 'I'."
                    }
                  ],
                  "help": [
                    {
                      "diagnosticId": "ZBZ001",
                      "title": "Interfaces must start with I"
                    }
                  ]
                }
                """)
            });

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ZBZ001", diagnostic.Id);
        Assert.Contains("Repository", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Analyzer_does_not_report_ZBZ001_for_interface_that_matches_cached_rule()
    {
        var diagnostics = await new AnalyzerTestHost().GetDiagnosticsAsync(
            "public interface IRepository { }",
            new ZerberuzAnalyzer(),
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
                        "mustStartWith": "I",
                        "mustMatch": "^I[A-Z].*"
                      },
                      "message": "Interface '{symbolName}' must start with 'I'."
                    }
                  ],
                  "help": [
                    {
                      "diagnosticId": "ZBZ001",
                      "title": "Interfaces must start with I"
                    }
                  ]
                }
                """)
            });

        Assert.Empty(diagnostics);
    }
}
