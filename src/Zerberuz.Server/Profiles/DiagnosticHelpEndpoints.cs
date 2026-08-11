using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Profiles;

public static class DiagnosticHelpEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticHelpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/diagnostics/{diagnosticId}", async (
            string diagnosticId,
            IProfileRuleStore store,
            CancellationToken cancellationToken) =>
        {
            var help = await store.FindHelpAsync(diagnosticId, cancellationToken).ConfigureAwait(false);
            return help is null
                ? Results.NotFound()
                : Results.Ok(new DiagnosticHelpResponse
                {
                    Help = help
                });
        });

        endpoints.MapGet("/api/v1/profiles/{profile}/versions/{version}/diagnostics/{diagnosticId}/help", async (
            string profile,
            string version,
            string diagnosticId,
            IProfileRuleStore store,
            CancellationToken cancellationToken) =>
        {
            var ruleSet = await store.GetRuleSetAsync(profile, version, cancellationToken).ConfigureAwait(false);
            var help = ruleSet?.Help.FirstOrDefault(candidate =>
                string.Equals(candidate.DiagnosticId, diagnosticId, StringComparison.Ordinal));

            return help is null
                ? Results.NotFound()
                : Results.Ok(new DiagnosticHelpResponse
                {
                    Profile = profile,
                    RulesVersion = version,
                    Help = help
                });
        });

        return endpoints;
    }
}
