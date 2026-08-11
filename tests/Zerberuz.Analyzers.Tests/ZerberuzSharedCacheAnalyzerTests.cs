using Zerberuz.Analyzers;
using global::Zerberuz.Analyzers.Configuration;

namespace Zerberuz.Analyzers.Tests;

public sealed class ZerberuzSharedCacheAnalyzerTests
{
    [Fact]
    public async Task Analyzer_loads_rules_from_shared_cache_using_project_configuration()
    {
        var team = "elysium-test-" + Guid.NewGuid().ToString("N");
        var profile = "backend";
        var configuration = new ZerberuzProjectConfiguration
        {
            Team = team,
            Profile = profile,
            RulesVersion = "latest-compatible"
        };
        var paths = new SharedCachePathResolver().Resolve(configuration);
        try
        {
            var versionPaths = SharedRuleCachePaths.Create(
                paths.CacheRoot,
                team,
                profile,
                "2026.08.11");

            Directory.CreateDirectory(Path.GetDirectoryName(versionPaths.RulesCachePath)!);
            File.WriteAllText(versionPaths.RulesCachePath, """
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

            Directory.CreateDirectory(Path.GetDirectoryName(paths.LatestCompatiblePointerPath)!);
            File.WriteAllText(paths.LatestCompatiblePointerPath, $$"""
            {
              "team": "{{team}}",
              "profile": "{{profile}}",
              "rulesVersion": "2026.08.11",
              "rulesCachePath": "{{versionPaths.RulesCachePath.Replace("\\", "\\\\")}}"
            }
            """);

            var diagnostics = await new AnalyzerTestHost().GetDiagnosticsAsync(
                "public interface Repository { }",
                new ZerberuzAnalyzer(),
                additionalFiles: new[]
                {
                    new InMemoryAdditionalText("zerberuz.json", $$"""
                    {
                      "team": "{{team}}",
                      "profile": "{{profile}}",
                      "rulesVersion": "latest-compatible"
                    }
                    """)
                });

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("ZBZ001", diagnostic.Id);
        }
        finally
        {
            var teamDirectory = Path.Combine(paths.CacheRoot, "teams", team);
            if (Directory.Exists(teamDirectory))
            {
                Directory.Delete(teamDirectory, recursive: true);
            }
        }
    }
}
