using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Zerberuz.Server.Contracts;

namespace Zerberuz.Server.Tests;

public sealed class DiagnosticHelpEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public DiagnosticHelpEndpointTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Global_diagnostic_help_endpoint_returns_seeded_help()
    {
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<DiagnosticHelpResponse>("/api/v1/diagnostics/ZBZ001");

        Assert.NotNull(response);
        Assert.Equal("ZBZ001", response.Help.DiagnosticId);
        Assert.Contains("Interface", response.Help.Title);
    }

    [Fact]
    public async Task Profile_diagnostic_help_endpoint_returns_seeded_help()
    {
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<DiagnosticHelpResponse>(
            "/api/v1/profiles/backend/versions/2026.08.11/diagnostics/ZBZ100/help");

        Assert.NotNull(response);
        Assert.Equal("backend", response.Profile);
        Assert.Equal("2026.08.11", response.RulesVersion);
        Assert.Equal("ZBZ100", response.Help.DiagnosticId);
    }

    [Fact]
    public async Task Missing_diagnostic_help_returns_not_found()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/diagnostics/ZBZ999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
