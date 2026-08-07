using Reference.Ui.Selenium.Infrastructure;

namespace Reference.Ui.Selenium.Tests;

/// <summary>
/// Authenticated render sweep — Selenium port of the Playwright suite's AllPagesRenderTests.
/// Every non-parameterised route must load in a real browser WITHOUT redirecting to /login and
/// WITHOUT an error boundary / failure banner. This is the regression net for the whole UI
/// (catches the "provider not found / empty page" class of bug that API tests are blind to).
/// Parameterised routes (detail/edit by id/name) are covered by the per-area interaction tests,
/// which reach them by clicking real rows.
/// </summary>
[Collection("ui-selenium")]
[Trait("Category", "Ui")]
public sealed class RouteRenderTests(SeleniumFixture fixture)
{
    // Why: this is the route inventory reference-ui actually maps (kept in lockstep with the
    // Playwright suite's AllPagesRenderTests.Routes — one denominator for both suites). Routes
    // the app does not wire (/orchestration, /projects, /terminal) are excluded; genuinely broken
    // pages stay in the list and stay RED until fixed (RED-on-bug convention).
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
    public void PageRendersAuthenticatedWithoutError(string route)
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewAuthenticatedDriver();
        driver.Navigate().GoToUrl($"{SeleniumFixture.BaseUrl}{route}");
        PageAssertions.ShouldBeAuthenticated(driver);   // didn't bounce to /login
        PageAssertions.ShouldNotShowError(driver);      // no error boundary / failure banner
        PageAssertions.ShouldHaveMainContent(driver);   // real content, not a blank shell
    }
}
