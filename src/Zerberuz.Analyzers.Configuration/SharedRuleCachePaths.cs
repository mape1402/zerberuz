namespace Zerberuz.Analyzers.Configuration;

public sealed class SharedRuleCachePaths
{
    private SharedRuleCachePaths(
        string cacheRoot,
        string team,
        string profile,
        string rulesVersion,
        string profileDirectory,
        string versionDirectory,
        string rulesCachePath,
        string helpDirectory,
        string latestCompatiblePointerPath)
    {
        CacheRoot = cacheRoot;
        Team = team;
        Profile = profile;
        RulesVersion = rulesVersion;
        ProfileDirectory = profileDirectory;
        VersionDirectory = versionDirectory;
        RulesCachePath = rulesCachePath;
        HelpDirectory = helpDirectory;
        LatestCompatiblePointerPath = latestCompatiblePointerPath;
    }

    public string CacheRoot { get; }

    public string Team { get; }

    public string Profile { get; }

    public string RulesVersion { get; }

    public string ProfileDirectory { get; }

    public string VersionDirectory { get; }

    public string RulesCachePath { get; }

    public string HelpDirectory { get; }

    public string LatestCompatiblePointerPath { get; }

    public static SharedRuleCachePaths Create(
        string cacheRoot,
        string team,
        string profile,
        string rulesVersion)
    {
        var normalizedCacheRoot = NormalizeRoot(cacheRoot);
        var normalizedTeam = NormalizeSegment(team);
        var normalizedProfile = NormalizeSegment(profile);
        var normalizedVersion = NormalizeSegment(rulesVersion);

        var profileDirectory = Path.Combine(
            normalizedCacheRoot,
            "teams",
            normalizedTeam,
            "profiles",
            normalizedProfile);

        var versionDirectory = Path.Combine(
            profileDirectory,
            "versions",
            normalizedVersion);

        return new SharedRuleCachePaths(
            normalizedCacheRoot,
            normalizedTeam,
            normalizedProfile,
            normalizedVersion,
            profileDirectory,
            versionDirectory,
            Path.Combine(versionDirectory, "rules-cache.json"),
            Path.Combine(versionDirectory, "help"),
            Path.Combine(profileDirectory, "latest-compatible.json"));
    }

    private static string NormalizeRoot(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cache root is required.", nameof(value));
        }

        return Path.GetFullPath(value);
    }

    private static string NormalizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cache path segments cannot be empty.", nameof(value));
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var normalized = new string(value
            .Trim()
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray());

        return normalized.Replace(' ', '-');
    }
}
