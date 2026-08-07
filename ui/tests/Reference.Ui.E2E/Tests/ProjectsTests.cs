using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// ETL Projects area (<c>/projects</c>, <c>/projects/list</c>, <c>/projects/edit</c>,
/// <c>/projects/edit/{id}</c>, <c>/projects/execution/{id}</c>).
///
/// Projects IS routed in this host: the pages come from the Fdw.Services.Etl.Projects.UI.Pages
/// package (scanned via the router's AdditionalAssemblies), so each path renders its real page (headings
/// "Project Tree", "Projects", "New Project", …) — NOT a 404 and NOT the NotFound fragment. The backend,
/// however, is absent: the reference-api slot exposes no <c>projects</c> CRUD route, so the rendered pages
/// have no working data path. These tests pin both facts.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class ProjectsTests(PlaywrightFixture fx)
{
    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("/projects", "Project Tree")]
    [InlineData("/projects/list", "Projects")]
    [InlineData("/projects/edit", "New Project")]
    public async Task ProjectRouteRendersItsPage(string route, string heading)
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
    public async Task ProjectCrudAbsentFromApi()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // The data path is dead independently of routing — the reference-api slot serves no `projects` CRUD
        // endpoint. Proven through the seeding channel (same api the UI uses).
        await using var seed = await ApiSeeder.CreateAsync();
        (await seed.RouteExistsAsync("projects")).ShouldBeFalse(
            "reference-api should expose NO `projects` CRUD route (confirming the dead Projects data path).");
    }
}
