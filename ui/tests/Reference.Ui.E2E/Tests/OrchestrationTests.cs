using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// ETL Orchestration (node tree) area (<c>/orchestration</c>, <c>/orchestration/edit</c>,
/// <c>/orchestration/edit/{id}</c>).
///
/// Orchestration IS routed in this host: the pages come from the Fdw.Services.Etl.Projects.UI.Pages
/// package (scanned via the router's AdditionalAssemblies), so each path renders its real page (heading
/// "Orchestration", etc.) — NOT a 404 and NOT the NotFound fragment. The backend, however, is absent: the
/// reference-api slot serves no <c>nodes</c> route (node endpoints live only in reference-etl as
/// <c>etl/nodes…</c>), so the rendered pages have no working data path. These tests pin both facts.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class OrchestrationTests(PlaywrightFixture fx)
{
    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("/orchestration", "Orchestration")]
    [InlineData("/orchestration/edit", "New Node")]
    public async Task OrchestrationRouteRendersItsPage(string route, string heading)
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // The route resolves to a real routed page (authenticated, NOT the NotFound fallback).
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}{route}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldBeAuthenticatedAsync(page);
            (await page.GetByText("Resource Not Found").CountAsync()).ShouldBe(0);
            await Assertions.Expect(page.Locator("h1").Filter(new() { HasTextString = heading }).First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task NodeCrudUnreachableFromApi()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // The UI's NodeApiClient targets the `nodes` route, which the reference-api slot does not serve (404)
        // — node endpoints live only in reference-etl as `etl/nodes…`. Proven via the seeding channel.
        await using var seed = await ApiSeeder.CreateAsync();
        (await seed.RouteExistsAsync("nodes")).ShouldBeFalse(
            "reference-api should expose NO `nodes` CRUD route (confirming the dead Orchestration data path).");
    }
}
