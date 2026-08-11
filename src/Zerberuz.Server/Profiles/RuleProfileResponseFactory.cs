using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Profiles;

public sealed class RuleProfileResponseFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RuleProfileResponse Create(RuleSetDefinition ruleSet)
    {
        var json = JsonSerializer.Serialize(ruleSet, JsonOptions);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));

        return new RuleProfileResponse
        {
            Profile = ruleSet.Profile,
            RulesVersion = ruleSet.RulesVersion,
            SchemaVersion = ruleSet.SchemaVersion,
            MinimumEngineVersion = ruleSet.MinimumEngineVersion,
            Sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant(),
            RuleSet = ruleSet
        };
    }
}
