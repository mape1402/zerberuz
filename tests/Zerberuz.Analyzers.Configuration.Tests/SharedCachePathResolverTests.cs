using Zerberuz.Analyzers.Configuration;

namespace Zerberuz.Analyzers.Configuration.Tests;

public sealed class SharedCachePathResolverTests
{
    [Fact]
    public void Resolve_prefers_cli_override()
    {
        var paths = CreateResolver(environmentCacheRoot: "C:/env/cache")
            .Resolve(
                new ZerberuzProjectConfiguration
                {
                    Team = "elysium",
                    Profile = "backend",
                    RulesVersion = "2026.08.11",
                    CacheRoot = "C:/config/cache"
                },
                cacheRootOverride: "C:/cli/cache");

        Assert.Equal(Path.GetFullPath("C:/cli/cache"), paths.CacheRoot);
        Assert.EndsWith(
            Path.Combine("teams", "elysium", "profiles", "backend", "versions", "2026.08.11", "rules-cache.json"),
            paths.RulesCachePath);
    }

    [Fact]
    public void Resolve_prefers_config_over_environment()
    {
        var paths = CreateResolver(environmentCacheRoot: "C:/env/cache")
            .Resolve(new ZerberuzProjectConfiguration
            {
                Team = "elysium",
                Profile = "backend",
                RulesVersion = "2026.08.11",
                CacheRoot = "C:/config/cache"
            });

        Assert.Equal(Path.GetFullPath("C:/config/cache"), paths.CacheRoot);
    }

    [Fact]
    public void Resolve_uses_environment_when_config_has_no_cache_root()
    {
        var paths = CreateResolver(environmentCacheRoot: "C:/env/cache")
            .Resolve(new ZerberuzProjectConfiguration
            {
                Team = "elysium",
                Profile = "backend",
                RulesVersion = "2026.08.11"
            });

        Assert.Equal(Path.GetFullPath("C:/env/cache"), paths.CacheRoot);
    }

    [Fact]
    public void Resolve_uses_os_default_when_no_overrides_exist()
    {
        var paths = CreateResolver(environmentCacheRoot: null)
            .Resolve(new ZerberuzProjectConfiguration
            {
                Team = "elysium",
                Profile = "backend",
                RulesVersion = "2026.08.11"
            });

        Assert.Equal(Path.GetFullPath("C:/local/Zerberuz/cache"), paths.CacheRoot);
    }

    [Fact]
    public void Load_reads_project_configuration_json()
    {
        var configuration = ZerberuzProjectConfiguration.Load("""
        {
          "team": "elysium",
          "profile": "backend-clean-architecture",
          "rulesVersion": "2026.08.11",
          "mode": "locked",
          "rulesEndpoint": "https://rules.example.test",
          "cacheRoot": "C:/zerberuz-cache"
        }
        """);

        Assert.Equal("elysium", configuration.Team);
        Assert.Equal("backend-clean-architecture", configuration.Profile);
        Assert.Equal("2026.08.11", configuration.RulesVersion);
        Assert.Equal("locked", configuration.Mode);
        Assert.Equal("https://rules.example.test", configuration.RulesEndpoint);
        Assert.Equal("C:/zerberuz-cache", configuration.CacheRoot);
    }

    private static SharedCachePathResolver CreateResolver(string? environmentCacheRoot)
    {
        return new SharedCachePathResolver(
            name => name == "ZERBERUZ_CACHE_ROOT" ? environmentCacheRoot : null,
            folder => folder == Environment.SpecialFolder.LocalApplicationData ? "C:/local" : string.Empty,
            () => "C:/users/tester");
    }
}
