using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Tests;

public sealed class RuleProfileEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly WebApplicationFactory<Program> factory;

    public RuleProfileEndpointTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Versions_endpoint_returns_seeded_profile_versions()
    {
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<ProfileVersionsResponse>(
            "/api/v1/profiles/backend/versions",
            JsonOptions);

        Assert.NotNull(response);
        Assert.Equal("backend", response.Profile);
        Assert.Contains("2026.08.11", response.Versions);
    }

    [Fact]
    public async Task Version_endpoint_returns_rule_profile_with_sha256()
    {
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<RuleProfileResponse>(
            "/api/v1/profiles/backend/versions/2026.08.11",
            JsonOptions);

        Assert.NotNull(response);
        Assert.Equal("backend", response.Profile);
        Assert.Equal("2026.08.11", response.RulesVersion);
        Assert.Equal(64, response.Sha256.Length);
        Assert.Contains(response.RuleSet.Rules, rule => rule.Id == "ZBZ001");
    }

    [Fact]
    public async Task Latest_compatible_endpoint_returns_seeded_profile()
    {
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<RuleProfileResponse>(
            "/api/v1/profiles/backend/latest-compatible?engineVersion=1.0.0",
            JsonOptions);

        Assert.NotNull(response);
        Assert.Equal("2026.08.11", response.RulesVersion);
    }

    [Fact]
    public async Task Unknown_profile_returns_not_found()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/profiles/missing/versions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publish_version_persists_rule_profile()
    {
        var client = factory.CreateClient();
        var ruleSet = CreateRuleSet("published-" + Guid.NewGuid().ToString("N"), "2026.09.01");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/profiles/{ruleSet.Profile}/versions",
            ruleSet,
            JsonOptions);
        var published = await response.Content.ReadFromJsonAsync<RuleProfileResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(published);
        Assert.Equal(ruleSet.Profile, published.Profile);
        Assert.Equal("2026.09.01", published.RulesVersion);

        var fetched = await client.GetFromJsonAsync<RuleProfileResponse>(
            $"/api/v1/profiles/{ruleSet.Profile}/versions/2026.09.01",
            JsonOptions);

        Assert.NotNull(fetched);
        Assert.Equal(ruleSet.Profile, fetched.Profile);
    }

    [Fact]
    public async Task Publish_version_returns_conflict_for_duplicate_profile_version()
    {
        var client = factory.CreateClient();
        var ruleSet = CreateRuleSet("duplicate-" + Guid.NewGuid().ToString("N"), "2026.09.01");

        var first = await client.PostAsJsonAsync(
            $"/api/v1/profiles/{ruleSet.Profile}/versions",
            ruleSet,
            JsonOptions);
        var second = await client.PostAsJsonAsync(
            $"/api/v1/profiles/{ruleSet.Profile}/versions",
            ruleSet,
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Publish_version_rejects_route_profile_mismatch()
    {
        var client = factory.CreateClient();
        var ruleSet = CreateRuleSet("contracts", "2026.09.01");

        var response = await client.PostAsJsonAsync(
            "/api/v1/profiles/backend/versions",
            ruleSet,
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private static RuleSetDefinition CreateRuleSet(string profile, string version)
    {
        return new RuleSetDefinition
        {
            SchemaVersion = "1.0",
            RulesVersion = version,
            Profile = profile,
            Rules =
            {
                new RuleDefinition
                {
                    Id = "ZBZ001",
                    Type = ZerberuzRuleType.Naming,
                    Title = "Interfaces must start with I",
                    Severity = ZerberuzDiagnosticSeverity.Warning,
                    Target = new RuleTargetDefinition
                    {
                        SymbolKind = ZerberuzSymbolKind.Interface
                    },
                    Condition = new RuleConditionDefinition
                    {
                        MustStartWith = "I"
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
}
