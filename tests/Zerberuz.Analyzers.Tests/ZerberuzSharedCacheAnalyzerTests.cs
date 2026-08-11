using Zerberuz.Analyzers;

namespace Zerberuz.Analyzers.Tests;

public sealed class ZerberuzSharedCacheAnalyzerTests
{
    [Fact]
    public async Task Analyzer_loads_rules_from_shared_cache_using_project_configuration()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "zerberuz-analyzer-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var cacheRoot = Path.Combine(tempRoot, "cache");
            var rulesCachePath = Path.Combine(
                cacheRoot,
                "teams",
                "elysium",
                "profiles",
                "backend",
                "versions",
                "2026.08.11",
                "rules-cache.json");

            var pointerPath = Path.Combine(
                cacheRoot,
                "teams",
                "elysium",
                "profiles",
                "backend",
                "latest-compatible.json");

            Directory.CreateDirectory(Path.GetDirectoryName(rulesCachePath)!);
            File.WriteAllText(rulesCachePath, """
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
                }
              ],
              "help": [
                {
                  "diagnosticId": "ZBZ001",
                  "title": "Interfaces must start with I"
                }
              ]
            }
            """);

            Directory.CreateDirectory(Path.GetDirectoryName(pointerPath)!);
            File.WriteAllText(pointerPath, $$"""
            {
              "team": "elysium",
              "profile": "backend",
              "rulesVersion": "2026.08.11",
              "rulesCachePath": "{{rulesCachePath.Replace("\\", "\\\\")}}"
            }
            """);

            var diagnostics = await new AnalyzerTestHost().GetDiagnosticsAsync(
                "public interface Repository { }",
                new ZerberuzAnalyzer(),
                additionalFiles: new[]
                {
                    new InMemoryAdditionalText("zerberuz.json", $$"""
                    {
                      "team": "elysium",
                      "profile": "backend",
                      "rulesVersion": "latest-compatible",
                      "cacheRoot": "{{cacheRoot.Replace("\\", "\\\\")}}"
                    }
                    """)
                });

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("ZBZ001", diagnostic.Id);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
