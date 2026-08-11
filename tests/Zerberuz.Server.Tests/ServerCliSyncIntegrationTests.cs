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

    private static string ReadFromTestServer(HttpClient client, string source)
    {
        var uri = new Uri(source);
        return client.GetStringAsync(uri.PathAndQuery).GetAwaiter().GetResult();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "zerberuz-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
