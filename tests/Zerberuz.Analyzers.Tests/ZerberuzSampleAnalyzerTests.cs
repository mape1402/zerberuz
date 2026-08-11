using Zerberuz.Analyzers;
using global::Zerberuz.Analyzers.Configuration;

namespace Zerberuz.Analyzers.Tests;

public sealed class ZerberuzSampleAnalyzerTests
{
    [Fact]
    public async Task Basic_sample_reports_configured_diagnostics_from_shared_cache()
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
            var repositoryRoot = FindRepositoryRoot();
            var samplePath = Path.Combine(
                repositoryRoot,
                "samples",
                "Zerberuz.Samples.Basic",
                "Program.cs");

            var versionPaths = SharedRuleCachePaths.Create(
                paths.CacheRoot,
                team,
                profile,
                "2026.08.11");

            Directory.CreateDirectory(Path.GetDirectoryName(versionPaths.RulesCachePath)!);
            File.WriteAllText(versionPaths.RulesCachePath, RuleSetWithNamingAndFolderRules);

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
                File.ReadAllText(samplePath),
                new ZerberuzAnalyzer(),
                filePath: samplePath,
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

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ZBZ001");
            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ZBZ100");
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

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Zerberuz.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find Zerberuz repository root.");
    }

    private const string RuleSetWithNamingAndFolderRules = """
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
    """;
}
