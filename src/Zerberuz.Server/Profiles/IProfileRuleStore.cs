using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Profiles;

public interface IProfileRuleStore
{
    Task<IReadOnlyCollection<string>> GetVersionsAsync(
        string profile,
        CancellationToken cancellationToken = default);

    Task<RuleSetDefinition?> GetRuleSetAsync(
        string profile,
        string version,
        CancellationToken cancellationToken = default);

    Task<RuleSetDefinition?> GetLatestCompatibleRuleSetAsync(
        string profile,
        string engineVersion,
        CancellationToken cancellationToken = default);

    Task<DiagnosticHelpDefinition?> FindHelpAsync(
        string diagnosticId,
        CancellationToken cancellationToken = default);
}
