using System.Text.Json;

namespace Zerberuz.Analyzers.Configuration;

public sealed class SharedRuleCacheResolver
{
    public string? ResolveRulesCachePath(
        ZerberuzProjectConfiguration configuration,
        SharedRuleCachePaths paths,
        Func<string, bool> fileExists,
        Func<string, string> readAllText)
    {
        if (!string.Equals(configuration.RulesVersion, "latest-compatible", StringComparison.OrdinalIgnoreCase))
        {
            return paths.RulesCachePath;
        }

        if (!fileExists(paths.LatestCompatiblePointerPath))
        {
            return paths.LatestCompatiblePointerPath;
        }

        using var pointer = JsonDocument.Parse(readAllText(paths.LatestCompatiblePointerPath));
        if (pointer.RootElement.TryGetProperty("RulesCachePath", out var pascalPath))
        {
            return pascalPath.GetString();
        }

        return pointer.RootElement.TryGetProperty("rulesCachePath", out var camelPath)
            ? camelPath.GetString()
            : null;
    }
}
