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

    public Task<IReadOnlyCollection<string>> GetProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> result = profiles.Keys.OrderBy(profile => profile).ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<string>> GetVersionsAsync(
        string profile,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> result = profiles.TryGetValue(profile, out var versions)
            ? versions.Keys.Reverse().ToArray()
            : Array.Empty<string>();

        return Task.FromResult(result);
    }

    public Task<RuleSetDefinition?> GetRuleSetAsync(
        string profile,
        string version,
        CancellationToken cancellationToken = default)
    {
        var result = profiles.TryGetValue(profile, out var versions) &&
            versions.TryGetValue(version, out var ruleSet)
                ? ruleSet
                : null;

        return Task.FromResult(result);
    }

    public Task<RuleSetDefinition?> GetLatestCompatibleRuleSetAsync(
        string profile,
        string engineVersion,
        CancellationToken cancellationToken = default)
    {
        var result = profiles.TryGetValue(profile, out var versions)
            ? versions.Values.LastOrDefault()
            : null;

        return Task.FromResult(result);
    }

    public Task<DiagnosticHelpDefinition?> FindHelpAsync(
        string diagnosticId,
        CancellationToken cancellationToken = default)
    {
        var result = profiles.Values
            .SelectMany(versions => versions.Values)
            .SelectMany(ruleSet => ruleSet.Help)
            .FirstOrDefault(help => string.Equals(help.DiagnosticId, diagnosticId, StringComparison.Ordinal));

        return Task.FromResult(result);
    }

    public Task<RuleProfilePublishResult> PublishRuleSetAsync(
        RuleSetDefinition ruleSet,
        CancellationToken cancellationToken = default)
    {
        if (!profiles.TryGetValue(ruleSet.Profile, out var versions))
        {
            versions = new SortedDictionary<string, RuleSetDefinition>(StringComparer.OrdinalIgnoreCase);
            profiles[ruleSet.Profile] = versions;
        }

        if (versions.ContainsKey(ruleSet.RulesVersion))
        {
            return Task.FromResult(RuleProfilePublishResult.Conflict(
                "ZBZP001",
                $"Rule profile '{ruleSet.Profile}@{ruleSet.RulesVersion}' already exists."));
        }

        versions[ruleSet.RulesVersion] = ruleSet;
        return Task.FromResult(RuleProfilePublishResult.Success(ruleSet));
    }
}
