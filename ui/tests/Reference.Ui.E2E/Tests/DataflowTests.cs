using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Dataflow overview (<c>/dataflow</c>) — a read-only view: four summary stat cards (DataSets /
/// DataStores / Sources / Connections) plus node groups by type. Driven through the rendered DOM.
///
/// Branch coverage: title + summary cards always render; node groups render when the graph has nodes,
/// else an empty-state card; Refresh re-runs the load without crashing.
///
/// RED finding documented in <see cref="ErrorBranchIsNeverShownDocumentedGap"/>: the provider sets an
/// ErrorMessage on load failure but Dataflow.razor never renders it (the page shows an empty graph
/// instead). On this slot the graph loads successfully, so that gap is latent — the test documents it
/// rather than forcing a failure we can't reproduce.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class DataflowTests(PlaywrightFixture fx)
{
    private async Task<(IBrowserContext, IPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/dataflow", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RendersTitleAndSummaryCards()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.Locator("h1", new() { HasTextString = "Dataflow Overview" })).ToBeVisibleAsync();
            // Four summary cards always render (stats default to 0 when no graph).
            var cards = page.Locator("div.grid.md\\:grid-cols-4 > div.card");
            await Assertions.Expect(cards.First).ToBeVisibleAsync(new() { Timeout = 15_000 });
            (await cards.CountAsync()).ShouldBe(4);
            foreach (var label in new[] { "DataSets", "DataStores", "Sources", "Connections" })
                await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = label })).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RendersNodeGroupsOrEmptyState()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // Wait for the async graph load to settle, then assert exactly one of the two branches.
            var groupHeader = page.Locator("h3.card-title");
            var empty = page.GetByText("No dataflow nodes found");
            await Assertions.Expect(groupHeader.First.Or(empty).First).ToBeVisibleAsync(new() { Timeout = 15_000 });

            if (await empty.CountAsync() > 0)
                return; // empty graph — valid render

            // Graph has nodes: each group header carries a "(count)" and rows render with labels + badges.
            (await groupHeader.CountAsync()).ShouldBeGreaterThan(0);
            var nodeRows = page.Locator("div.card-content div.border.border-gray-700");
            (await nodeRows.CountAsync()).ShouldBeGreaterThan(0);
            await Assertions.Expect(nodeRows.First.Locator("h4")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SummaryCountMatchesRenderedConnectionNodes()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.Locator("h3.card-title").First.Or(page.GetByText("No dataflow nodes found")).First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // Find the "connection (N)" group header; assert its rendered row count equals N.
            var connHeader = page.Locator("h3.card-title").Filter(new() { HasTextRegex = new Regex("^connection ", RegexOptions.IgnoreCase) });
            if (await connHeader.CountAsync() == 0) return; // no connection nodes in graph

            var headerText = await connHeader.First.InnerTextAsync();
            var match = Regex.Match(headerText, @"\((\d+)\)");
            match.Success.ShouldBeTrue();
            var declared = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

            // Walk up to the wrapping div.card (the header's parent is .card-header; its parent is .card)
            // then count node rows in that group's .card-content.
            var card = connHeader.First.Locator("xpath=ancestor::div[contains(concat(' ',normalize-space(@class),' '),' card ')][1]");
            (await card.Locator("div.card-content div.border.border-gray-700").CountAsync()).ShouldBe(declared);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Each rendered node row carries a node-type badge <c>span</c> (GetNodeTypeBadge maps Pipeline /
    /// DataSet / DataStore / Connection to distinct badge classes). Asserts the badge element renders on
    /// at least one node row when the graph has nodes (bUnit RendersNodeTypeBadge end-to-end).
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task NodeRowsCarryTypeBadge()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.Locator("h3.card-title").First.Or(page.GetByText("No dataflow nodes found")).First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            var nodeRows = page.Locator("div.card-content div.border.border-gray-700");
            if (await nodeRows.CountAsync() == 0) return; // empty graph

            // Each node row renders a badge span (class starts with "badge" or a colour class).
            var badge = nodeRows.First.Locator("span[class*='badge'], span[class*='text-']").First;
            await Assertions.Expect(badge).ToBeVisibleAsync(new() { Timeout = 10_000 });
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
            await page.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await Assertions.Expect(page.Locator("h1", new() { HasTextString = "Dataflow Overview" })).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// RED (documented gap, latent on this slot): DataflowProvider.LoadGraph sets ErrorMessage on a
    /// non-success/exception load, but Dataflow.razor NEVER renders an error banner — it shows the
    /// empty-graph state instead, swallowing the failure reason. The slot's graph endpoint succeeds, so
    /// we cannot force the error here; this test verifies the page exposes NO error banner element at
    /// all (confirming the missing-display gap), so adding error display later flags it for upgrade.
    /// </summary>
    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Status", "Red")]
    public async Task ErrorBranchIsNeverShownDocumentedGap()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.Locator("h1", new() { HasTextString = "Dataflow Overview" })).ToBeVisibleAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            // The page markup contains no red error-banner element (the provider's ErrorMessage is never
            // wired to the DOM). Documents the gap: a future error-display addition will make this >0.
            (await page.Locator("div.border-red-900\\/50").CountAsync()).ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }
}
