namespace Zerberuz.Analyzers.Configuration;

public sealed class ZerberuzConfigurationPathResolver
{
    private readonly Func<Environment.SpecialFolder, string> getFolderPath;
    private readonly Func<string> getUserProfilePath;

    public ZerberuzConfigurationPathResolver()
        : this(
            Environment.GetFolderPath,
            () => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
    {
    }

    public ZerberuzConfigurationPathResolver(
        Func<Environment.SpecialFolder, string> getFolderPath,
        Func<string> getUserProfilePath)
    {
        this.getFolderPath = getFolderPath;
        this.getUserProfilePath = getUserProfilePath;
    }

    public string ResolveGlobalConfigurationPath()
    {
        var commonApplicationData = getFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (!string.IsNullOrWhiteSpace(commonApplicationData))
        {
            return Path.Combine(commonApplicationData, "Zerberuz", "zerberuz.json");
        }

        return Path.Combine(getUserProfilePath(), ".zerberuz", "zerberuz.json");
    }
}
