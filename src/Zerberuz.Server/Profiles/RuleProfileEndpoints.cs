using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Profiles;

public static class RuleProfileEndpoints
{
    public static IEndpointRouteBuilder MapRuleProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/profiles/{profile}/versions", async (
            string profile,
            IProfileRuleStore store,
            CancellationToken cancellationToken) =>
        {
            var versions = await store.GetVersionsAsync(profile, cancellationToken).ConfigureAwait(false);
            return versions.Count == 0
                ? Results.NotFound()
                : Results.Ok(new ProfileVersionsResponse
                {
                    Profile = profile,
                    Versions = versions.ToList()
                });
        });

        endpoints.MapGet("/api/v1/profiles/{profile}/versions/{version}", async (
            string profile,
            string version,
            IProfileRuleStore store,
            RuleProfileResponseFactory factory,
            CancellationToken cancellationToken) =>
        {
            var ruleSet = await store.GetRuleSetAsync(profile, version, cancellationToken).ConfigureAwait(false);
            return ruleSet is null
                ? Results.NotFound()
                : Results.Ok(factory.Create(ruleSet));
        });

        endpoints.MapGet("/api/v1/profiles/{profile}/latest-compatible", async (
            string profile,
            string? engineVersion,
            IProfileRuleStore store,
            RuleProfileResponseFactory factory,
            CancellationToken cancellationToken) =>
        {
            var ruleSet = await store.GetLatestCompatibleRuleSetAsync(
                    profile,
                    engineVersion ?? "1.0.0",
                    cancellationToken)
                .ConfigureAwait(false);

            return ruleSet is null
                ? Results.NotFound()
                : Results.Ok(factory.Create(ruleSet));
        });

        return endpoints;
    }
}
