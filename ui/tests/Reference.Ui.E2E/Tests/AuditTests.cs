using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// /audit read-only execution-history view, driven through the rendered DOM. Renders an HTML table
/// (<c>table tbody tr</c> per execution) with State + Item-Type dropdown filters, an Apply Filters and a
/// Refresh button, and a "No execution history found" empty state. Audit data is produced by pipeline
/// runs (not seedable through the admin API), so these tests assert table STRUCTURE and the
/// FILTER-CONTRACT rather than an exact ambient row count.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class AuditTests(PlaywrightFixture fx)
{
    private async Task<(IBrowserContext, IPage)> OpenAuditAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/audit", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    private static ILocator Rows(IPage page) => page.Locator("table tbody tr");

    /// <summary>The page renders without error and shows either the execution table or the empty state.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task AuditRendersTableOrEmptyState()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAuditAsync();
        try
        {
            await page.WaitForTimeoutAsync(2000);
            var rows = await Rows(page).CountAsync();
            if (rows == 0)
                await Assertions.Expect(page.GetByText(new Regex("No execution history found"))).ToBeVisibleAsync();
            else
                // Each row carries the State/Type badge columns — the footer reports the count.
                await Assertions.Expect(page.GetByText(new Regex(@"Showing \d+ of \d+ executions"))).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>The State and Item-Type filter dropdowns render with their full documented option sets.</summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task FilterDropdownsExposeAllOptions()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAuditAsync();
        try
        {
            var stateOptions = await page.Locator("select").First.Locator("option").AllInnerTextsAsync();
            stateOptions.ShouldContain("Succeeded");
            stateOptions.ShouldContain("Failed");
            var typeOptions = await page.Locator("select").Nth(1).Locator("option").AllInnerTextsAsync();
            typeOptions.ShouldContain("Workflow");
            typeOptions.ShouldContain("Job");
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// KNOWN BUG (RED) — the State/Type dropdowns <c>@bind</c> the CONTEXT, but Apply Filters / Refresh
    /// call <c>ctx.OnRefresh</c> (which reads the PROVIDER's own filter fields), NOT
    /// <c>OnFilterChanged</c>. So a chosen filter value is never pushed into the query: the result set is
    /// inert. We assert the CORRECT behavior — picking a State whose value matches NONE of the loaded
    /// rows must empty the table — which currently FAILS, documenting the inert-filter bug in
    /// <c>Audit.razor</c> (Apply/Refresh → OnRefresh instead of OnFilterChanged).
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task StateFilterNarrowsResultsKnownBugInertFilter()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAuditAsync();
        try
        {
            await page.WaitForTimeoutAsync(2000);
            var before = await Rows(page).CountAsync();
            if (before == 0) { Assert.Skip("No ambient executions to filter; cannot exercise the filter contract."); return; }

            // The ambient executions in this slot are all state 'Scheduled'. The "Succeeded" option carries
            // the domain state value 'Completed' (ExecutionStateTypes has no "Succeeded"); selecting it should
            // narrow the table to 0 rows since no ambient row is Completed.
            await page.Locator("select").First.SelectOptionAsync(new SelectOptionValue { Value = "Completed" });
            await page.GetByRole(AriaRole.Button, new() { Name = "Apply Filters" }).ClickAsync();
            await page.WaitForTimeoutAsync(2000);
            await PageAssertions.ShouldNotShowErrorAsync(page);

            // CORRECT behavior: a State=Succeeded filter excludes the Scheduled rows → empty table.
            (await Rows(page).CountAsync()).ShouldBe(0,
                "Audit State filter is inert (Apply/Refresh call OnRefresh, not OnFilterChanged) — known bug.");
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>Refresh reloads the view without throwing an error boundary.</summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task RefreshReloadsWithoutError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAuditAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }
}
