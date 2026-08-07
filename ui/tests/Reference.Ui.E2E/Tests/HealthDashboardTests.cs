using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// System Health dashboard (<c>/health</c>) — a read-only monitoring view driven by
/// <c>HealthDashboardProvider</c> (GET <c>health/system</c> + per-service throughput). Renders a header
/// with a REFRESH button, an overall-status banner (green Healthy / red Unhealthy / loading-pulse /
/// error), a service-status card grid (response gauge, uptime, last-check, optional Req/s + Errors), and
/// optional Throughput Details panels. Driven through the rendered DOM.
///
/// The dashboard data is produced by the live monitoring backend (not seedable through the admin API),
/// so these tests assert STRUCTURE and the documented render BRANCHES against whatever terminal state
/// the slot settles into (services grid, or loading/error), never an exact ambient service count.
/// bUnit-covered branches that need backend-shaped fixtures to force (per-service throughput presence,
/// high-error-rate colour, uptime day/hour/minute formatting) are listed as bUnit-only in the report.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class HealthDashboardTests(PlaywrightFixture fx)
{
    private static readonly RegexOptions Ci = RegexOptions.IgnoreCase;

    private async Task<(IBrowserContext, IPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/health", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RendersHeaderAndRefresh()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("System Health", Ci) }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("REFRESH", Ci) }))
                .ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// The provider settles into exactly one terminal branch: the overall-status banner (Healthy or
    /// Unhealthy, when SystemHealth loaded), the loading-pulse, or the error banner. Asserts one of those
    /// is visible — i.e. the page reaches a defined state without a crash.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task SettlesIntoBannerOrLoadingOrError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // Healthy/Unhealthy banner carries a "N service(s) monitored" line; loading uses an
            // animate-pulse text; error uses the red-500/50 banner. Any one of them is a valid terminal.
            var banner = page.GetByText(new Regex(@"service\(s\) monitored", Ci));
            var loading = page.GetByText(new Regex("Loading system health", Ci));
            var error = page.Locator("div.border-red-500\\/50");
            await Assertions.Expect(banner.Or(loading).Or(error).First)
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// When SystemHealth loads with services, the service-status grid renders one card per service
    /// (each with the service name + a status label). If the slot returns no snapshot, the loading/error
    /// branch is asserted instead — both are valid renders, neither is a crash.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task ServiceGridRendersCardsWhenSnapshotPresent()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.WaitForTimeoutAsync(3000);
            var banner = page.GetByText(new Regex(@"service\(s\) monitored", Ci));
            if (await banner.CountAsync() > 0)
            {
                // Snapshot present → at least one service card with a response gauge (svg) is rendered.
                var cards = page.Locator("div.card.bg-gray-800\\/5");
                (await cards.CountAsync()).ShouldBeGreaterThan(0);
                (await page.Locator("div.card svg").CountAsync()).ShouldBeGreaterThan(0);
            }
            else
            {
                // No snapshot → loading-pulse or error banner is the terminal state.
                var loading = page.GetByText(new Regex("Loading system health", Ci));
                var error = page.Locator("div.border-red-500\\/50");
                await Assertions.Expect(loading.Or(error).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
            }
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task RefreshReloadsWithoutCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("REFRESH", Ci) }).ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("System Health", Ci) }))
                .ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }
}
