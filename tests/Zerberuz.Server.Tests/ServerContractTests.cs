using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Tests;

public sealed class ServerContractTests
{
    [Fact]
    public void RuleProfileResponse_carries_rule_set_and_hash_metadata()
    {
        var response = new RuleProfileResponse
        {
            Profile = "backend",
            RulesVersion = "2026.08.11",
            SchemaVersion = "1.0",
            MinimumEngineVersion = "1.0.0",
            Sha256 = "abc123",
            RuleSet = new RuleSetDefinition
            {
                Profile = "backend",
                RulesVersion = "2026.08.11"
            }
        };

        Assert.Equal("backend", response.Profile);
        Assert.Equal("2026.08.11", response.RuleSet.RulesVersion);
        Assert.Equal("abc123", response.Sha256);
    }
}
