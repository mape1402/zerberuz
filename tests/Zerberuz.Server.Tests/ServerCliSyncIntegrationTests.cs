using Microsoft.AspNetCore.Mvc.Testing;
using Zerberuz.Analyzers.Configuration;
using Zerberuz.Cli;

namespace Zerberuz.Server.Tests;

public sealed class ServerCliSyncIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ServerCliSyncIntegrationTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void Sync_rules_downloads_server_profile_into_shared_cache()
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
              "rulesVersion": "latest-compatible",
              "mode": "latest-compatible",
              "rulesEndpoint": "http://zerberuz.test"
            }
            """);

            using var client = factory.CreateClient();
            var output = new StringWriter();
            var error = new StringWriter();

            var exitCode = new CliApplication().Run(
                new[]
                {
                    "sync-rules",
                    "--server",
                    "http://zerberuz.test",
                    "--profile",
                    "backend",
                    "--config-path",
                    configPath
                },
                output,
                error,
                File.Exists,
                File.ReadAllText,
                readSource: source => ReadFromTestServer(client, source),
                resolveCachePaths: configuration => SharedRuleCachePaths.Create(
                    cacheRoot,
                    configuration.Team,
                    configuration.Profile,
                    configuration.RulesVersion));

            var rulesCachePath = Path.Combine(
                cacheRoot,
                "teams",
                "elysium",
                "profiles",
                "backend",
                "versions",
                "2026.08.11",
                "rules-cache.json");

            var latestCompatiblePath = Path.Combine(
                cacheRoot,
                "teams",
                "elysium",
                "profiles",
                "backend",
                "latest-compatible.json");

            Assert.Equal(0, exitCode);
            Assert.Contains("Synced elysium/backend@2026.08.11", output.ToString());
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(rulesCachePath));
            Assert.True(File.Exists(latestCompatiblePath));
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
            Assert.True(File.Exists(Path.Combine(
                cacheRoot,
                "teams",
                "elysium",
                "profiles",
                "backend",
                "versions",
                "2026.08.11",
                "help",
                "ZBZ100.md")));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Publish_then_sync_rules_round_trips_through_server_and_cli()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var profile = "cli-published-" + Guid.NewGuid().ToString("N");
            var configPath = Path.Combine(tempRoot, "zerberuz.json");
            var ruleSetPath = Path.Combine(tempRoot, "rules.json");
            var cacheRoot = Path.Combine(tempRoot, "cache");

            File.WriteAllText(configPath, $$"""
            {
              "team": "elysium",
              "profile": "{{profile}}",
              "rulesVersion": "latest-compatible",
              "mode": "latest-compatible",
              "rulesEndpoint": "http://zerberuz.test"
            }
            """);

            File.WriteAllText(ruleSetPath, $$"""
            {
              "schemaVersion": "1.0",
              "rulesVersion": "2026.10.01",
              "profile": "{{profile}}",
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

            using var client = factory.CreateClient();
            var publishOutput = new StringWriter();
            var publishError = new StringWriter();
            var publishExitCode = new CliApplication().Run(
                new[]
                {
                    "rules",
                    "publish",
                    ruleSetPath,
                    "--server",
                    "http://zerberuz.test"
                },
                publishOutput,
                publishError,
                File.Exists,
                File.ReadAllText,
                postJson: (url, payload) => PostToTestServer(client, url, payload));

            var syncOutput = new StringWriter();
            var syncError = new StringWriter();
            var syncExitCode = new CliApplication().Run(
                new[]
                {
                    "sync-rules",
                    "--server",
                    "http://zerberuz.test",
                    "--profile",
                    profile,
                    "--config-path",
                    configPath
                },
                syncOutput,
                syncError,
                File.Exists,
                File.ReadAllText,
                readSource: source => ReadFromTestServer(client, source),
                resolveCachePaths: configuration => SharedRuleCachePaths.Create(
                    cacheRoot,
                    configuration.Team,
                    configuration.Profile,
                    configuration.RulesVersion));

            var rulesCachePath = Path.Combine(
                cacheRoot,
                "teams",
                "elysium",
                "profiles",
                profile,
                "versions",
                "2026.10.01",
                "rules-cache.json");

            Assert.Equal(0, publishExitCode);
            Assert.Contains($"Published {profile}@2026.10.01", publishOutput.ToString());
            Assert.Equal(string.Empty, publishError.ToString());
            Assert.Equal(0, syncExitCode);
            Assert.True(File.Exists(rulesCachePath));
            Assert.Contains($"Synced elysium/{profile}@2026.10.01", syncOutput.ToString());
            Assert.Equal(string.Empty, syncError.ToString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string ReadFromTestServer(HttpClient client, string source)
    {
        var uri = new Uri(source);
        return client.GetStringAsync(uri.PathAndQuery).GetAwaiter().GetResult();
    }

    private static string PostToTestServer(HttpClient client, string source, string payload)
    {
        var uri = new Uri(source);
        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var response = client.PostAsync(uri.PathAndQuery, content).GetAwaiter().GetResult();
        var responsePayload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return responsePayload;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "zerberuz-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
