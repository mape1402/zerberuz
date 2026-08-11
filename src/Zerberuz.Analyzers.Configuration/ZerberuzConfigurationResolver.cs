namespace Zerberuz.Analyzers.Configuration;

public sealed class ZerberuzConfigurationResolver
{
    private readonly ZerberuzConfigurationPathResolver pathResolver;

    public ZerberuzConfigurationResolver()
        : this(new ZerberuzConfigurationPathResolver())
    {
    }

    public ZerberuzConfigurationResolver(ZerberuzConfigurationPathResolver pathResolver)
    {
        this.pathResolver = pathResolver;
    }

    public ZerberuzProjectConfiguration Resolve(
        string? projectConfigurationJson,
        Func<string, bool> fileExists,
        Func<string, string> readAllText)
    {
        if (!string.IsNullOrWhiteSpace(projectConfigurationJson))
        {
            return ZerberuzProjectConfiguration.Load(projectConfigurationJson!);
        }

        var globalPath = pathResolver.ResolveGlobalConfigurationPath();
        if (!fileExists(globalPath))
        {
            return new ZerberuzProjectConfiguration();
        }

        var globalJson = readAllText(globalPath);
        return ZerberuzProjectConfiguration.Load(globalJson);
    }
}
