using Zerberuz.Server.Contracts;
using Zerberuz.Analyzers.Rules;

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

        endpoints.MapPost("/api/v1/profiles/{profile}/versions", async (
            string profile,
            RuleSetDefinition ruleSet,
            IProfileRuleStore store,
            RuleProfileResponseFactory factory,
            CancellationToken cancellationToken) =>
        {
            if (!string.Equals(profile, ruleSet.Profile, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new RuleProfilePublishErrorResponse
                {
                    Code = "ZBZP002",
                    Message = "Route profile must match the rule set profile."
                });
            }

            var validation = new RuleSetValidator().Validate(ruleSet);
            if (!validation.IsValid)
            {
                return Results.BadRequest(new RuleValidationResponse
                {
                    IsValid = false,
                    Errors = validation.Errors
                        .Select(error => new RuleValidationErrorResponse
                        {
                            Code = error.Code,
                            Message = error.Message
                        })
                        .ToList()
                });
            }

            var publishResult = await store.PublishRuleSetAsync(ruleSet, cancellationToken).ConfigureAwait(false);
            if (!publishResult.Succeeded)
            {
                return Results.Conflict(new RuleProfilePublishErrorResponse
                {
                    Code = publishResult.ErrorCode,
                    Message = publishResult.ErrorMessage
                });
            }

            return Results.Created(
                $"/api/v1/profiles/{ruleSet.Profile}/versions/{ruleSet.RulesVersion}",
                factory.Create(ruleSet));
        });

        return endpoints;
    }
}
