using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Profiles;

public interface IProfileRuleStore
{
    IReadOnlyCollection<string> GetVersions(string profile);

    RuleSetDefinition? GetRuleSet(string profile, string version);

    RuleSetDefinition? GetLatestCompatibleRuleSet(string profile, string engineVersion);

    DiagnosticHelpDefinition? FindHelp(string diagnosticId);
}
