using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Profiles;

public static class RuleProfileEndpoints
{
    public static IEndpointRouteBuilder MapRuleProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/profiles/{profile}/versions", (string profile, IProfileRuleStore store) =>
        {
            var versions = store.GetVersions(profile);
            return versions.Count == 0
                ? Results.NotFound()
                : Results.Ok(new ProfileVersionsResponse
                {
                    Profile = profile,
                    Versions = versions.ToList()
                });
        });

        endpoints.MapGet("/api/v1/profiles/{profile}/versions/{version}", (
            string profile,
            string version,
            IProfileRuleStore store,
            RuleProfileResponseFactory factory) =>
        {
            var ruleSet = store.GetRuleSet(profile, version);
            return ruleSet is null
                ? Results.NotFound()
                : Results.Ok(factory.Create(ruleSet));
        });

        endpoints.MapGet("/api/v1/profiles/{profile}/latest-compatible", (
            string profile,
            string? engineVersion,
            IProfileRuleStore store,
            RuleProfileResponseFactory factory) =>
        {
            var ruleSet = store.GetLatestCompatibleRuleSet(profile, engineVersion ?? "1.0.0");
            return ruleSet is null
                ? Results.NotFound()
                : Results.Ok(factory.Create(ruleSet));
        });

        return endpoints;
    }
}
