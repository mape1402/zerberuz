using Zerberuz.Cli;

namespace Zerberuz.Cli.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public void Run_init_creates_default_configuration()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var writes = new Dictionary<string, string>(StringComparer.Ordinal);
        var exitCode = new CliApplication().Run(
            new[] { "init", "--profile", "backend-clean-architecture" },
            output,
            error,
            path => writes.ContainsKey(path),
            _ => string.Empty,
            (path, content) => writes[path] = content);

        Assert.Equal(0, exitCode);
        Assert.Contains("Created zerberuz.json", output.ToString());
        Assert.Contains("\"team\": \"default\"", writes["zerberuz.json"]);
        Assert.Contains("backend-clean-architecture", writes["zerberuz.json"]);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void Run_sync_rules_writes_valid_rules_to_shared_cache()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var configPath = Path.Combine(tempRoot, "zerberuz.json");
            var sourcePath = Path.Combine(tempRoot, "rules.json");
            var cacheRoot = Path.Combine(tempRoot, "cache");

            File.WriteAllText(configPath, """
            {
              "team": "elysium",
              "profile": "backend",
              "rulesVersion": "latest-compatible",
              "mode": "latest-compatible",
              "rulesEndpoint": "https://rules.example.test"
            }
            """);

            File.WriteAllText(sourcePath, ValidRuleSet);

            var output = new StringWriter();
            var error = new StringWriter();
            var exitCode = new CliApplication().Run(
                new[] { "sync-rules", "--source", sourcePath, "--config-path", configPath, "--cache-root", cacheRoot },
                output,
                error,
                File.Exists,
                File.ReadAllText);

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

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(rulesCachePath));
            Assert.True(File.Exists(pointerPath));
            Assert.True(File.Exists(Path.Combine(
                cacheRoot,
                "teams",
                "elysium",
                "profiles",
                "backend",
                "versions",
                "2026.08.11",
                "help",
                "ZBZ001.md")));
            Assert.Contains("Synced elysium/backend@2026.08.11", output.ToString());
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Run_sync_rules_does_not_replace_cache_when_source_is_invalid()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var configPath = Path.Combine(tempRoot, "zerberuz.json");
            var sourcePath = Path.Combine(tempRoot, "rules.json");
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

            Directory.CreateDirectory(Path.GetDirectoryName(rulesCachePath)!);
            File.WriteAllText(rulesCachePath, "existing-cache");

            File.WriteAllText(configPath, """
            {
              "team": "elysium",
              "profile": "backend",
              "rulesVersion": "latest-compatible"
            }
            """);

            File.WriteAllText(sourcePath, """
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
                    "mustMatch": "["
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

            var output = new StringWriter();
            var error = new StringWriter();
            var exitCode = new CliApplication().Run(
                new[] { "sync-rules", "--source", sourcePath, "--config-path", configPath, "--cache-root", cacheRoot },
                output,
                error,
                File.Exists,
                File.ReadAllText);

            Assert.Equal(8, exitCode);
            Assert.Equal("existing-cache", File.ReadAllText(rulesCachePath));
            Assert.Contains("Rule source is invalid", error.ToString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Run_doctor_reports_healthy_shared_cache()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var configPath = Path.Combine(tempRoot, "zerberuz.json");
            var sourcePath = Path.Combine(tempRoot, "rules.json");
            var cacheRoot = Path.Combine(tempRoot, "cache");

            File.WriteAllText(configPath, """
            {
              "team": "elysium",
              "profile": "backend",
              "rulesVersion": "latest-compatible"
            }
            """);

            File.WriteAllText(sourcePath, ValidRuleSet);

            _ = new CliApplication().Run(
                new[] { "sync-rules", "--source", sourcePath, "--config-path", configPath, "--cache-root", cacheRoot },
                new StringWriter(),
                new StringWriter(),
                File.Exists,
                File.ReadAllText);

            var output = new StringWriter();
            var error = new StringWriter();
            var exitCode = new CliApplication().Run(
                new[] { "doctor", "--config-path", configPath, "--cache-root", cacheRoot },
                output,
                error,
                File.Exists,
                File.ReadAllText);

            Assert.Equal(0, exitCode);
            Assert.Contains("Status: healthy", output.ToString());
            Assert.Contains("Resolved rules version: 2026.08.11", output.ToString());
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Run_doctor_returns_error_when_cache_is_missing()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var configPath = Path.Combine(tempRoot, "zerberuz.json");
            var cacheRoot = Path.Combine(tempRoot, "cache");

            File.WriteAllText(configPath, """
            {
              "team": "elysium",
              "profile": "backend",
              "rulesVersion": "latest-compatible"
            }
            """);

            var output = new StringWriter();
            var error = new StringWriter();
            var exitCode = new CliApplication().Run(
                new[] { "doctor", "--config-path", configPath, "--cache-root", cacheRoot },
                output,
                error,
                File.Exists,
                File.ReadAllText);

            Assert.Equal(9, exitCode);
            Assert.Contains("Resolved rule cache was not found", error.ToString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Run_explain_offline_reads_markdown_help_from_shared_cache()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var configPath = Path.Combine(tempRoot, "zerberuz.json");
            var sourcePath = Path.Combine(tempRoot, "rules.json");
            var cacheRoot = Path.Combine(tempRoot, "cache");

            File.WriteAllText(configPath, """
            {
              "team": "elysium",
              "profile": "backend",
              "rulesVersion": "latest-compatible"
            }
            """);

            File.WriteAllText(sourcePath, ValidRuleSetWithDetailedHelp);

            _ = new CliApplication().Run(
                new[] { "sync-rules", "--source", sourcePath, "--config-path", configPath, "--cache-root", cacheRoot },
                new StringWriter(),
                new StringWriter(),
                File.Exists,
                File.ReadAllText);

            var output = new StringWriter();
            var error = new StringWriter();
            var exitCode = new CliApplication().Run(
                new[] { "explain", "ZBZ001", "--offline", "--config-path", configPath, "--cache-root", cacheRoot },
                output,
                error,
                File.Exists,
                File.ReadAllText);

            Assert.Equal(0, exitCode);
            Assert.Contains("# ZBZ001: Interfaces must start with I", output.ToString());
            Assert.Contains("Rename the interface.", output.ToString());
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Run_init_returns_error_when_configuration_exists()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = new CliApplication().Run(
            new[] { "init" },
            output,
            error,
            path => path == "zerberuz.json",
            _ => string.Empty,
            (_, _) => { });

        Assert.Equal(5, exitCode);
        Assert.Contains("Configuration already exists", error.ToString());
    }

    [Fact]
    public void Run_explain_renders_cached_help()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = new CliApplication().Run(
            new[] { "explain", "ZBZ001", "--cache-path", ".zerberuz/rules-cache.json" },
            output,
            error,
            path => path == ".zerberuz/rules-cache.json",
            _ => """
            {
              "schemaVersion": "1.0",
              "rulesVersion": "2026.08.11",
              "profile": "backend",
              "rules": [],
              "help": [
                {
                  "diagnosticId": "ZBZ001",
                  "title": "Interfaces must start with I",
                  "summary": "Interface names must use the configured prefix.",
                  "why": "Consistent interface naming improves scanning.",
                  "trigger": "An interface name did not match the rule.",
                  "badExample": "public interface Repository { }",
                  "goodExample": "public interface IRepository { }",
                  "fixSteps": [
                    "Rename the interface.",
                    "Update all references."
                  ],
                  "suppressionGuidance": "Suppress only for external contracts.",
                  "relatedDiagnostics": [ "ZBZ100" ]
                }
              ]
            }
            """);

        Assert.Equal(0, exitCode);
        Assert.Contains("ZBZ001: Interfaces must start with I", output.ToString());
        Assert.Contains("Rename the interface.", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void Run_explain_returns_error_when_cache_is_missing()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = new CliApplication().Run(
            new[] { "explain", "ZBZ001" },
            output,
            error,
            _ => false,
            _ => string.Empty);

        Assert.Equal(3, exitCode);
        Assert.Contains("Rule cache was not found", error.ToString());
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "zerberuz-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private const string ValidRuleSet = """
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
    """;

    private const string ValidRuleSetWithDetailedHelp = """
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
          "title": "Interfaces must start with I",
          "summary": "Interface names must use the configured prefix.",
          "why": "Consistent interface naming improves scanning.",
          "trigger": "An interface name did not match the rule.",
          "badExample": "public interface Repository { }",
          "goodExample": "public interface IRepository { }",
          "fixSteps": [
            "Rename the interface.",
            "Update all references."
          ],
          "suppressionGuidance": "Suppress only for external contracts.",
          "relatedDiagnostics": [ "ZBZ100" ]
        }
      ]
    }
    """;
}
