using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Comprehensive render sweep: every non-parameterised route must load in the browser as an
/// authenticated user WITHOUT redirecting to /login and WITHOUT an error boundary / failure banner.
/// This is the regression net for the whole UI (catches the "provider not found / empty page"
/// class of bug that API tests are blind to). Parameterised routes (detail/edit by id/name) are
/// covered by the per-area CRUD tests, which reach them by clicking real rows.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class AllPagesRenderTests(PlaywrightFixture fx)
{
    // Why: this is the route inventory reference-ui actually maps. Routes carried over from the FDW
    // *.UI.Pages packages but NOT wired into this app (/orchestration, /orchestration/edit, /projects,
    // /projects/edit, /projects/list, /terminal) return HTTP 404 and were removed — they are not pages
    // of this application. /messages and /access-requests ARE real pages and are intentionally kept:
    // they currently 500 (FDW MessageProvider / access-request provider mutates component state off the
    // Blazor Dispatcher during prerender) and this sweep is the regression net that holds them red
    // until that FDW defect is fixed.
    public static TheoryData<string> Routes() => new()
    {
        "/", "/access-requests", "/access-requests/new", "/agent-actions", "/api-keys", "/audit",
        "/calculations", "/calculations/new", "/catalog", "/configuration", "/configuration/issues",
        "/connections", "/connections/new", "/connectors", "/data-preview", "/dataflow",
        "/datasets", "/datasets/new", "/datasets/calculated/new", "/datastores", "/datastores/new",
        "/glossary", "/lineage", "/mapper", "/messages",
        "/pipelines", "/pipelines/new", "/profile",
        "/promotions", "/quality", "/quality/dashboard", "/quality/rules", "/roles", "/schedules",
        "/schedules/new", "/schema", "/schema/tables/new", "/secret-managers", "/settings",
        "/settings/appearance", "/settings/notifications", "/users",
    };

    [Theory]
    [MemberData(nameof(Routes))]
    [Trait("Priority", "P1")]
    public async Task PageRendersAuthenticatedWithoutError(string route)
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}{route}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldBeAuthenticatedAsync(page);   // didn't bounce to /login
            await PageAssertions.ShouldNotShowErrorAsync(page);      // no error boundary / failure banner
            // The page produced real content (not a blank shell).
            var main = page.Locator("main, [role=main], .page, body");
            (await main.First.InnerTextAsync()).Trim().ShouldNotBeNullOrEmpty();
        }
        finally
        {
            await ctx.CloseAsync();
        }
    }
}
