using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Zerberuz.Server.Tests;

public sealed class AdminUiPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AdminUiPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Admin_home_lists_seeded_profiles()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/zerberuz");

        Assert.Contains("Rule Profiles", html);
        Assert.Contains("backend", html);
        Assert.Contains("2026.08.11", html);
    }

    [Fact]
    public async Task Admin_version_page_shows_rule_summary()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/zerberuz/profiles/backend/versions/2026.08.11");

        Assert.Contains("backend", html);
        Assert.Contains("2026.08.11", html);
        Assert.Contains("ZBZ001", html);
        Assert.Contains("ZBZ100", html);
    }

    [Fact]
    public async Task Admin_publish_page_renders_publish_form()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/zerberuz/publish");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("New Profile Version", html);
        Assert.Contains("1. Profile", html);
        Assert.Contains("2. Rules", html);
        Assert.Contains("3. Help", html);
        Assert.Contains("4. Review", html);
        Assert.Contains("Add Another Rule", html);
        Assert.Contains("Publish Version", html);
    }
}
