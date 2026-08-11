using Zerberuz.Analyzers;

namespace Zerberuz.Analyzers.Tests;

public sealed class ZerberuzSampleAnalyzerTests
{
    [Fact]
    public async Task Basic_sample_reports_configured_diagnostics_from_shared_cache()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "zerberuz-sample-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var repositoryRoot = FindRepositoryRoot();
            var samplePath = Path.Combine(
                repositoryRoot,
                "samples",
                "Zerberuz.Samples.Basic",
                "Program.cs");

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
            File.WriteAllText(rulesCachePath, RuleSetWithNamingAndFolderRules);

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
                File.ReadAllText(samplePath),
                new ZerberuzAnalyzer(),
                filePath: samplePath,
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

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ZBZ001");
            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ZBZ100");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
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
