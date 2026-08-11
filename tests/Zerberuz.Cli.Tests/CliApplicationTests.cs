using Zerberuz.Cli;

namespace Zerberuz.Cli.Tests;

public sealed class CliApplicationTests
{
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
}
