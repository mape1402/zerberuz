using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Profiles;

public static class DiagnosticHelpEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticHelpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/diagnostics/{diagnosticId}", (
            string diagnosticId,
            IProfileRuleStore store) =>
        {
            var help = store.FindHelp(diagnosticId);
            return help is null
                ? Results.NotFound()
                : Results.Ok(new DiagnosticHelpResponse
                {
                    Help = help
                });
        });

        endpoints.MapGet("/api/v1/profiles/{profile}/versions/{version}/diagnostics/{diagnosticId}/help", (
            string profile,
            string version,
            string diagnosticId,
            IProfileRuleStore store) =>
        {
            var ruleSet = store.GetRuleSet(profile, version);
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
