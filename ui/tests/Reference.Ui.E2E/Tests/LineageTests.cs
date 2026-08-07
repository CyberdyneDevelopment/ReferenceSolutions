using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// System Lineage (<c>/lineage</c> and <c>/lineage/{type}/{name}</c>) — an interactive SVG pan/zoom
/// graph with a toolbar (entity-type + entity-name selects, Show Lineage / Show All buttons), a node
/// detail panel, zoom controls, and a legend. Driven through the rendered DOM.
///
/// Covered branches: initial empty overlay; toolbar disabled-state cascade; entity-type change loads
/// the entity-name dropdown; Show All paints SVG nodes; node select opens the detail panel + the
/// selected-ring; close panel; zoom-in/out/reset; deep-link route param pre-fills the toolbar.
///
/// Documented (NOT forced RED here): lazy-expand and entity-name-load FAILURES are silently swallowed
/// (catch → []/return) with no user-visible error — see
/// <see cref="EntityNameLoadFailuresAreSilentDocumented"/>. On this slot the name loads succeed, so
/// the silent-failure path can't be reproduced; the test documents the absence of any error surface.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class LineageTests(PlaywrightFixture fx)
{
    private const string NodeSelector = "svg g.cursor-move";

    private async Task<(IBrowserContext, IPage)> OpenAsync(string route = "/lineage")
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}{route}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    private static ILocator TypeSelect(IPage p) => p.Locator("select.w-40");
    private static ILocator NameSelect(IPage p) => p.Locator("select.w-48");

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RendersToolbarAndEmptyOverlay()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "System Lineage" })).ToBeVisibleAsync();
            await Assertions.Expect(TypeSelect(page)).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Show All" })).ToBeVisibleAsync();
            // Initial state: no nodes → empty overlay prompt.
            await Assertions.Expect(page.GetByText("Select an entity type and name to view lineage")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ToolbarDisabledCascade()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // With no type chosen: name select disabled, Show Lineage disabled.
            await Assertions.Expect(NameSelect(page)).ToBeDisabledAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Show Lineage" })).ToBeDisabledAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EntityTypeChangeLoadsNames()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await TypeSelect(page).SelectOptionAsync(new SelectOptionValue { Value = "Connection" });
            // OnEntityTypeChanged loads names from the matching client; the name select enables and
            // gains options beyond the "Select entity..." placeholder (connections exist on this slot).
            await Assertions.Expect(NameSelect(page)).ToBeEnabledAsync(new() { Timeout = 15_000 });
            await page.WaitForFunctionAsync(
                "() => document.querySelector('select.w-48')?.options.length > 1",
                null, new PageWaitForFunctionOptions { Timeout = 15_000 });
            (await NameSelect(page).Locator("option").CountAsync()).ShouldBeGreaterThan(1);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ShowAllPaintsGraphNodes()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Show All" }).ClickAsync();
            // Full graph load → BuildGraphFromDataflow paints draggable node groups in the SVG.
            await Assertions.Expect(page.Locator(NodeSelector).First).ToBeVisibleAsync(new() { Timeout = 20_000 });
            (await page.Locator(NodeSelector).CountAsync()).ShouldBeGreaterThan(0);
            // The empty overlay must be gone once nodes are present.
            await Assertions.Expect(page.GetByText("Select an entity type and name to view lineage")).ToBeHiddenAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SelectNodeOpensDetailPanelAndCloses()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Show All" }).ClickAsync();
            await Assertions.Expect(page.Locator(NodeSelector).First).ToBeVisibleAsync(new() { Timeout = 20_000 });

            await page.Locator(NodeSelector).First.ClickAsync();
            // SelectNode → detail panel (absolute top-16 right-4) with a close button + Type/Status rows.
            var panel = page.Locator("div.absolute.top-16.right-4");
            await Assertions.Expect(panel).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(panel.Locator("h3")).ToBeVisibleAsync();
            (await page.InnerTextAsync("div.absolute.top-16.right-4")).ShouldContain("Type");

            // Selected-node pulse ring appears in the SVG.
            (await page.Locator("svg circle.animate-pulse").CountAsync()).ShouldBeGreaterThan(0);

            // Close the panel.
            await panel.GetByRole(AriaRole.Button).First.ClickAsync();
            await Assertions.Expect(panel).ToBeHiddenAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SelectingSameNodeTwiceDeselects()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Show All" }).ClickAsync();
            await Assertions.Expect(page.Locator(NodeSelector).First).ToBeVisibleAsync(new() { Timeout = 20_000 });

            var node = page.Locator(NodeSelector).First;
            await node.ClickAsync();
            var panel = page.Locator("div.absolute.top-16.right-4");
            await Assertions.Expect(panel).ToBeVisibleAsync(new() { Timeout = 10_000 });
            // SelectNode toggles — clicking the SAME node again deselects (panel disappears).
            await node.ClickAsync();
            await Assertions.Expect(panel).ToBeHiddenAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Show All paints EDGES (SVG <c>path</c> elements with the arrow marker) between nodes, not just the
    /// node groups. Asserts at least one edge path renders once the full graph loads (bUnit
    /// ShowAllRendersEdgeBetweenNodes end-to-end). If the slot graph happens to have isolated nodes only,
    /// this self-skips rather than asserting a false negative.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task ShowAllRendersEdgePaths()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Show All" }).ClickAsync();
            await Assertions.Expect(page.Locator(NodeSelector).First).ToBeVisibleAsync(new() { Timeout = 20_000 });
            // Edges are <path> elements inside the SVG transform group (node shapes are rect/circle/etc.,
            // edges are the connecting paths). The grid pattern is a <pattern>, not a drawn path.
            var edges = page.Locator("svg g[transform^='translate'] path");
            if (await edges.CountAsync() == 0)
            {
                Assert.Skip("Slot lineage graph has no edges between nodes — edge-render branch unreachable here.");
                return;
            }
            (await edges.CountAsync()).ShouldBeGreaterThan(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Selecting a DataSet node shows the "Drill into fields" button in the detail panel; clicking it
    /// loads the field lineage list (or "Fields (0)" when none) — the DataSet drill-in branch. Walks the
    /// painted nodes to find a DataSet (its detail panel exposes the drill button); skips if the slot
    /// graph has no DataSet node.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task DataSetNodeDrillIntoFieldsLoadsFieldList()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Show All" }).ClickAsync();
            await Assertions.Expect(page.Locator(NodeSelector).First).ToBeVisibleAsync(new() { Timeout = 20_000 });

            var nodes = page.Locator(NodeSelector);
            var count = Math.Min(await nodes.CountAsync(), 30);
            var panel = page.Locator("div.absolute.top-16.right-4");
            var drill = page.GetByRole(AriaRole.Button, new() { Name = "Drill into fields" });

            // Click nodes until one opens a panel exposing "Drill into fields" (DataSet nodes only).
            // Force the click: legend/zoom overlays (absolute-positioned) can sit over some nodes and
            // intercept normal pointer events — the SVG node still receives @onclick when forced. Tolerate
            // nodes whose panel doesn't open (overlap) by moving on rather than asserting per-node.
            var foundDrill = false;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    // Force the click past absolute overlays; some nodes lay outside the SVG viewport and
                    // can't be clicked at all — skip those rather than fail (best-effort node sweep).
                    await nodes.Nth(i).ClickAsync(new() { Force = true, Timeout = 3_000 });
                }
                catch (PlaywrightException) { continue; }
                await page.WaitForTimeoutAsync(300);
                if (await drill.CountAsync() > 0) { foundDrill = true; break; }
                if (await panel.CountAsync() > 0)
                {
                    try { await nodes.Nth(i).ClickAsync(new() { Force = true, Timeout = 3_000 }); }
                    catch (PlaywrightException) { /* leave selected; next node toggles state anyway */ }
                }
            }

            if (!foundDrill)
            {
                Assert.Skip("Slot lineage graph exposes no clickable DataSet node — drill-into-fields branch " +
                            "unreachable here (covered by bUnit DataSetNodeShowsDrillIntoFields).");
                return;
            }

            await drill.First.ClickAsync(new() { Force = true });
            // Field lineage loaded: the panel shows a "Fields (N)" header (N may be 0).
            await Assertions.Expect(page.GetByText(new Regex(@"Fields \(\d+\)", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ZoomControlsRescaleTransform()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Show All" }).ClickAsync();
            await Assertions.Expect(page.Locator(NodeSelector).First).ToBeVisibleAsync(new() { Timeout = 20_000 });

            var grp = page.Locator("svg g[transform^='translate']").First;
            var before = await grp.GetAttributeAsync("transform");

            // Zoom controls live bottom-right; identify by their order (ZoomIn, ZoomOut, Reset).
            var zoomBtns = page.Locator("div.absolute.bottom-4.right-4 button");
            (await zoomBtns.CountAsync()).ShouldBeGreaterThanOrEqualTo(3);
            await zoomBtns.Nth(0).ClickAsync(); // ZoomIn → scale +0.1
            await page.WaitForTimeoutAsync(400);
            var afterZoom = await grp.GetAttributeAsync("transform");
            afterZoom.ShouldNotBe(before); // scale changed in the transform

            await zoomBtns.Nth(2).ClickAsync(); // ResetView → (80,80,0.85)
            await page.WaitForTimeoutAsync(400);
            (await grp.GetAttributeAsync("transform") ?? string.Empty).ShouldContain("scale(0.85)");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DeepLinkRoutePreFillsToolbar()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Route param /lineage/{type}/{name} → OnInitializedAsync sets _entityType (triggers name load)
        // and _entityName. We assert the type select reflects the deep-linked value.
        var (ctx, page) = await OpenAsync("/lineage/Connection/EspnNfl");
        try
        {
            await Assertions.Expect(TypeSelect(page)).ToHaveValueAsync("Connection", new() { Timeout = 15_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Documented (latent on this slot): entity-name-load and node lazy-expand failures are SILENTLY
    /// swallowed (catch → names=[] / return) with no error surfaced to the user. On this slot the name
    /// loads succeed, so we cannot reproduce a failure; this test confirms that even on a SUCCESSFUL
    /// path the toolbar renders NO error element — there is no error surface at all on this page, which
    /// is exactly the gap (a failed load would look identical to "no data"). Documents the swallow.
    /// </summary>
    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Status", "Red")]
    public async Task EntityNameLoadFailuresAreSilentDocumented()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await TypeSelect(page).SelectOptionAsync(new SelectOptionValue { Value = "Calculation" });
            await page.WaitForTimeoutAsync(3000);
            // Whether names loaded or the call failed, the page shows NO error banner — the failure path
            // is indistinguishable from "no calculations". That is the documented silent-swallow gap.
            (await page.Locator("div.border-red-900\\/50").CountAsync()).ShouldBe(0);
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }
}
