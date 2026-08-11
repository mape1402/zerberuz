using Zerberuz.Analyzers.Configuration;

namespace Zerberuz.Analyzers.Configuration.Tests;

public sealed class SharedRuleCacheResolverTests
{
    [Fact]
    public void ResolveRulesCachePath_returns_pinned_version_path()
    {
        var paths = SharedRuleCachePaths.Create("C:/cache", "elysium", "backend", "2026.08.11");
        var resolved = new SharedRuleCacheResolver().ResolveRulesCachePath(
            new ZerberuzProjectConfiguration
            {
                RulesVersion = "2026.08.11"
            },
            paths,
            _ => false,
            _ => string.Empty);

        Assert.Equal(paths.RulesCachePath, resolved);
    }

    [Fact]
    public void ResolveRulesCachePath_reads_latest_compatible_pointer()
    {
        var paths = SharedRuleCachePaths.Create("C:/cache", "elysium", "backend", "latest-compatible");
        var resolved = new SharedRuleCacheResolver().ResolveRulesCachePath(
            new ZerberuzProjectConfiguration
            {
                RulesVersion = "latest-compatible"
            },
            paths,
            path => path == paths.LatestCompatiblePointerPath,
            _ => """
            {
              "team": "elysium",
              "profile": "backend",
              "rulesVersion": "2026.08.11",
              "rulesCachePath": "C:/cache/teams/elysium/profiles/backend/versions/2026.08.11/rules-cache.json"
            }
            """);

        Assert.Equal(
            "C:/cache/teams/elysium/profiles/backend/versions/2026.08.11/rules-cache.json",
            resolved);
    }
}
