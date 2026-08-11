using Zerberuz.Server.Profiles;

namespace Zerberuz.Server.Tests;

public sealed class InMemoryProfileRuleStoreTests
{
    [Fact]
    public async Task GetVersions_returns_seeded_backend_version()
    {
        var versions = await new InMemoryProfileRuleStore().GetVersionsAsync("backend");

        Assert.Contains("2026.08.11", versions);
    }

    [Fact]
    public async Task GetProfiles_returns_seeded_backend_profile()
    {
        var profiles = await new InMemoryProfileRuleStore().GetProfilesAsync();

        Assert.Contains("backend", profiles);
    }

    [Fact]
    public async Task GetRuleSet_returns_seeded_backend_rules()
    {
        var ruleSet = await new InMemoryProfileRuleStore().GetRuleSetAsync("backend", "2026.08.11");

        Assert.NotNull(ruleSet);
        Assert.Contains(ruleSet.Rules, rule => rule.Id == "ZBZ001");
        Assert.Contains(ruleSet.Rules, rule => rule.Id == "ZBZ100");
    }

    [Fact]
    public async Task FindHelp_returns_seeded_diagnostic_help()
    {
        var help = await new InMemoryProfileRuleStore().FindHelpAsync("ZBZ001");

        Assert.NotNull(help);
        Assert.Equal("Interfaces must start with I", help.Title);
    }
}
