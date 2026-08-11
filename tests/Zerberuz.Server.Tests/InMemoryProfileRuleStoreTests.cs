using Zerberuz.Server.Profiles;

namespace Zerberuz.Server.Tests;

public sealed class InMemoryProfileRuleStoreTests
{
    [Fact]
    public void GetVersions_returns_seeded_backend_version()
    {
        var versions = new InMemoryProfileRuleStore().GetVersions("backend");

        Assert.Contains("2026.08.11", versions);
    }

    [Fact]
    public void GetRuleSet_returns_seeded_backend_rules()
    {
        var ruleSet = new InMemoryProfileRuleStore().GetRuleSet("backend", "2026.08.11");

        Assert.NotNull(ruleSet);
        Assert.Contains(ruleSet.Rules, rule => rule.Id == "ZBZ001");
        Assert.Contains(ruleSet.Rules, rule => rule.Id == "ZBZ100");
    }

    [Fact]
    public void FindHelp_returns_seeded_diagnostic_help()
    {
        var help = new InMemoryProfileRuleStore().FindHelp("ZBZ001");

        Assert.NotNull(help);
        Assert.Equal("Interfaces must start with I", help.Title);
    }
}
