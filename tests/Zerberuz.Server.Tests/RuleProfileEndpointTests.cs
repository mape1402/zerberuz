using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Tests;

public sealed class RuleProfileEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public RuleProfileEndpointTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Versions_endpoint_returns_seeded_profile_versions()
    {
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<ProfileVersionsResponse>("/api/v1/profiles/backend/versions");

        Assert.NotNull(response);
        Assert.Equal("backend", response.Profile);
        Assert.Contains("2026.08.11", response.Versions);
    }

    [Fact]
    public async Task Version_endpoint_returns_rule_profile_with_sha256()
    {
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<RuleProfileResponse>(
            "/api/v1/profiles/backend/versions/2026.08.11");

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
            "/api/v1/profiles/backend/latest-compatible?engineVersion=1.0.0");

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
}
