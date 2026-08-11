using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Contracts;

public sealed class RuleProfileResponse
{
    public string Profile { get; set; } = string.Empty;

    public string RulesVersion { get; set; } = string.Empty;

    public string SchemaVersion { get; set; } = string.Empty;

    public string MinimumEngineVersion { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public RuleSetDefinition RuleSet { get; set; } = new();
}
