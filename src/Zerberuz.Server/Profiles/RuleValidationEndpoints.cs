using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Profiles;

public static class RuleValidationEndpoints
{
    public static IEndpointRouteBuilder MapRuleValidationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/rules/validate", (RuleSetDefinition ruleSet) =>
        {
            var validation = new RuleSetValidator().Validate(ruleSet);
            return Results.Ok(new RuleValidationResponse
            {
                IsValid = validation.IsValid,
                Errors = validation.Errors
                    .Select(error => new RuleValidationErrorResponse
                    {
                        Code = error.Code,
                        Message = error.Message
                    })
                    .ToList()
            });
        });

        return endpoints;
    }
}
