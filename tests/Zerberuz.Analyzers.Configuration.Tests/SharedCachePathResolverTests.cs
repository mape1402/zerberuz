using Zerberuz.Analyzers.Configuration;

namespace Zerberuz.Analyzers.Configuration.Tests;

public sealed class SharedCachePathResolverTests
{
    [Fact]
    public void Resolve_uses_common_application_data_cache_root()
    {
        var paths = CreateResolver(commonApplicationData: "C:/program-data")
            .Resolve(new ZerberuzProjectConfiguration
            {
                Team = "elysium",
                Profile = "backend",
                RulesVersion = "2026.08.11"
            });

        Assert.Equal(Path.GetFullPath("C:/program-data/Zerberuz/cache"), paths.CacheRoot);
        Assert.EndsWith(
            Path.Combine("teams", "elysium", "profiles", "backend", "versions", "2026.08.11", "rules-cache.json"),
            paths.RulesCachePath);
    }

    [Fact]
    public void Resolve_falls_back_to_user_profile_when_common_application_data_is_missing()
    {
        var paths = CreateResolver(commonApplicationData: string.Empty)
            .Resolve(new ZerberuzProjectConfiguration
            {
                Team = "elysium",
                Profile = "backend",
                RulesVersion = "2026.08.11"
            });

        Assert.Equal(Path.GetFullPath("C:/users/tester/.zerberuz/cache"), paths.CacheRoot);
    }

    [Fact]
    public void Load_reads_configuration_json_without_cache_root()
    {
        var configuration = ZerberuzProjectConfiguration.Load("""
        {
          "team": "elysium",
          "profile": "backend-clean-architecture",
          "rulesVersion": "2026.08.11",
          "mode": "locked",
          "rulesEndpoint": "https://rules.example.test"
        }
        """);

        Assert.Equal("elysium", configuration.Team);
        Assert.Equal("backend-clean-architecture", configuration.Profile);
        Assert.Equal("2026.08.11", configuration.RulesVersion);
        Assert.Equal("locked", configuration.Mode);
        Assert.Equal("https://rules.example.test", configuration.RulesEndpoint);
    }

    [Fact]
    public void Global_configuration_path_uses_common_application_data()
    {
        var path = new ZerberuzConfigurationPathResolver(
                folder => folder == Environment.SpecialFolder.CommonApplicationData ? "C:/program-data" : string.Empty,
                () => "C:/users/tester")
            .ResolveGlobalConfigurationPath();

        Assert.Equal(Path.GetFullPath("C:/program-data/Zerberuz/zerberuz.json"), Path.GetFullPath(path));
    }

    [Fact]
    public void Configuration_resolver_prefers_project_json_over_global_file()
    {
        var resolver = new ZerberuzConfigurationResolver(new ZerberuzConfigurationPathResolver(
            folder => folder == Environment.SpecialFolder.CommonApplicationData ? "C:/program-data" : string.Empty,
            () => "C:/users/tester"));

        var configuration = resolver.Resolve(
            """
            {
              "team": "project",
              "profile": "backend"
            }
            """,
            _ => true,
            _ => """
            {
              "team": "global",
              "profile": "backend"
            }
            """);

        Assert.Equal("project", configuration.Team);
    }

    private static SharedCachePathResolver CreateResolver(string commonApplicationData)
    {
        return new SharedCachePathResolver(
            folder => folder == Environment.SpecialFolder.CommonApplicationData ? commonApplicationData : string.Empty,
            () => "C:/users/tester");
    }
}
