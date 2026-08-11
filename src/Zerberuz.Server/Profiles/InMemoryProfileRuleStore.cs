using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Profiles;

public sealed class InMemoryProfileRuleStore : IProfileRuleStore
{
    private readonly Dictionary<string, SortedDictionary<string, RuleSetDefinition>> profiles;

    public InMemoryProfileRuleStore()
        : this(new[] { ProfileSeedData.CreateBackendRuleSet() })
    {
    }

    internal InMemoryProfileRuleStore(IEnumerable<RuleSetDefinition> ruleSets)
    {
        profiles = new Dictionary<string, SortedDictionary<string, RuleSetDefinition>>(StringComparer.OrdinalIgnoreCase);

        foreach (var ruleSet in ruleSets)
        {
            if (!profiles.TryGetValue(ruleSet.Profile, out var versions))
            {
                versions = new SortedDictionary<string, RuleSetDefinition>(StringComparer.OrdinalIgnoreCase);
                profiles[ruleSet.Profile] = versions;
            }

            versions[ruleSet.RulesVersion] = ruleSet;
        }
    }

    public IReadOnlyCollection<string> GetVersions(string profile)
    {
        return profiles.TryGetValue(profile, out var versions)
            ? versions.Keys.Reverse().ToArray()
            : Array.Empty<string>();
    }

    public RuleSetDefinition? GetRuleSet(string profile, string version)
    {
        return profiles.TryGetValue(profile, out var versions) &&
            versions.TryGetValue(version, out var ruleSet)
                ? ruleSet
                : null;
    }

    public RuleSetDefinition? GetLatestCompatibleRuleSet(string profile, string engineVersion)
    {
        return profiles.TryGetValue(profile, out var versions)
            ? versions.Values.LastOrDefault()
            : null;
    }

    public DiagnosticHelpDefinition? FindHelp(string diagnosticId)
    {
        return profiles.Values
            .SelectMany(versions => versions.Values)
            .SelectMany(ruleSet => ruleSet.Help)
            .FirstOrDefault(help => string.Equals(help.DiagnosticId, diagnosticId, StringComparison.Ordinal));
    }
}
