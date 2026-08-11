using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Tests;

public sealed class RuleValidationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly WebApplicationFactory<Program> factory;

    public RuleValidationEndpointTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Validate_endpoint_accepts_valid_rule_set()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/rules/validate", CreateValidRuleSet(), JsonOptions);
        var validation = await response.Content.ReadFromJsonAsync<RuleValidationResponse>(JsonOptions);

        Assert.NotNull(validation);
        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }

    [Fact]
    public async Task Validate_endpoint_reports_invalid_rule_set()
    {
        var client = factory.CreateClient();
        var ruleSet = CreateValidRuleSet();
        ruleSet.Rules[0].Condition.MustMatch = "[";

        var response = await client.PostAsJsonAsync("/api/v1/rules/validate", ruleSet, JsonOptions);
        var validation = await response.Content.ReadFromJsonAsync<RuleValidationResponse>(JsonOptions);

        Assert.NotNull(validation);
        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Code == "ZBZV016");
    }

    private static RuleSetDefinition CreateValidRuleSet()
    {
        return new RuleSetDefinition
        {
            SchemaVersion = "1.0",
            RulesVersion = "2026.08.11",
            Profile = "backend",
            Rules =
            {
                new RuleDefinition
                {
                    Id = "ZBZ001",
                    Type = ZerberuzRuleType.Naming,
                    Title = "Interfaces must start with I",
                    Target = new RuleTargetDefinition
                    {
                        SymbolKind = ZerberuzSymbolKind.Interface
                    },
                    Condition = new RuleConditionDefinition
                    {
                        MustMatch = "^I[A-Z].*"
                    },
                    Message = "Interface '{symbolName}' must start with 'I'."
                }
            },
            Help =
            {
                new DiagnosticHelpDefinition
                {
                    DiagnosticId = "ZBZ001",
                    Title = "Interfaces must start with I"
                }
            }
        };
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
