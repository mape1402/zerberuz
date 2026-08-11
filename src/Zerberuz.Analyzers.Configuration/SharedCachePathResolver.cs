namespace Zerberuz.Analyzers.Configuration;

public sealed class SharedCachePathResolver
{
    private readonly Func<string, string?> readEnvironmentVariable;
    private readonly Func<Environment.SpecialFolder, string> getFolderPath;
    private readonly Func<string> getUserProfilePath;

    public SharedCachePathResolver()
        : this(
            Environment.GetEnvironmentVariable,
            Environment.GetFolderPath,
            () => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
    {
    }

    public SharedCachePathResolver(
        Func<string, string?> readEnvironmentVariable,
        Func<Environment.SpecialFolder, string> getFolderPath,
        Func<string> getUserProfilePath)
    {
        this.readEnvironmentVariable = readEnvironmentVariable;
        this.getFolderPath = getFolderPath;
        this.getUserProfilePath = getUserProfilePath;
    }

    public SharedRuleCachePaths Resolve(
        ZerberuzProjectConfiguration configuration,
        string? cacheRootOverride = null)
    {
        var cacheRoot = FirstNonEmpty(
            cacheRootOverride,
            configuration.CacheRoot,
            readEnvironmentVariable("ZERBERUZ_CACHE_ROOT"),
            ResolveDefaultCacheRoot());

        return SharedRuleCachePaths.Create(
            cacheRoot,
            configuration.Team,
            configuration.Profile,
            configuration.RulesVersion);
    }

    private string ResolveDefaultCacheRoot()
    {
        var localApplicationData = getFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localApplicationData))
        {
            return Path.Combine(localApplicationData, "Zerberuz", "cache");
        }

        var userProfile = getUserProfilePath();
        return Path.Combine(userProfile, ".zerberuz", "cache");
    }

    private static string FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate!;
            }
        }

        throw new InvalidOperationException("A Zerberuz cache root could not be resolved.");
    }
}
