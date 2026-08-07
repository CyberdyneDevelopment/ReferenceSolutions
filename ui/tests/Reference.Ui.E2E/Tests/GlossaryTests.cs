using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Glossary (/glossary) — list + search + inline create + inline delete, driven through the rendered DOM.
/// The list is a <c>.grid.gap-3</c> of <c>.card</c> term cards (term in an <c>h3</c>, definition in a
/// <c>p</c>, a <c>Delete</c> button with NO confirm dialog). Create is an inline panel toggled by "New Term"
/// (Term + Definition inputs, "Create" button); search is a placeholder input + "Search" button hitting
/// <c>GET /catalog/glossary[/search]</c>.
///
/// CONFIRMED APP BUGS (see the RED tests):
///  * Glossary CREATE fails server-side (HTTP 500) — the red banner reads "GlossaryProvider: Failed to
///    create glossary term '{term}'" and the term never becomes a card. See <see cref="CreateTermFailsWithErrorBanner"/>.
///  * Existing persisted glossary rows render with an EMPTY term name (<c>&lt;h3&gt;&lt;/h3&gt;</c>) — the
///    definition text shows but the Term field is blank, evidence the create/persist path doesn't store
///    Term. See <see cref="PersistedTermsHaveBlankTermName"/>.
/// Because create is broken there is no working UI path to own a deterministic glossary fixture, so the
/// happy-path create→appears and create→delete→gone flows can't be made green; they are documented in the
/// RED tests. The blank-Term CLIENT no-op (a separate, correct behavior) IS asserted green.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class GlossaryTests(PlaywrightFixture fx)
{
    private const string CardSelector = ".grid.gap-3 > .card";

    private async Task<(IBrowserContext, IPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/glossary", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await page.WaitForTimeoutAsync(2_000); // circuit attach + async term load
        return (ctx, page);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListRendersHeaderAndControls()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await Assertions.Expect(page.Locator("h1").Filter(new() { HasTextRegex = new Regex("Glossary", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "New Term" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Refresh" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByPlaceholder(new Regex("Search glossary terms", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task RefreshReloadsListWithoutError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit RefreshButton_InvokesOnRefresh — clicking Refresh re-runs the list load (GET /catalog/glossary)
        // and the page settles with no error banner. Whether the slot holds ambient rows or is empty, the
        // page must come back to a healthy state (either term cards or the empty-state card).
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();
            await page.WaitForTimeoutAsync(2_000);
            await PageAssertions.ShouldNotShowErrorAsync(page);
            // Exactly one of: term cards present, OR the empty-state card.
            var cards = await page.Locator(CardSelector).CountAsync();
            var empty = await page.GetByText(new Regex("No glossary terms defined", RegexOptions.IgnoreCase)).CountAsync();
            (cards > 0 || empty > 0).ShouldBeTrue();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DeleteIsImmediateNoConfirmDialogSeedBlocked()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit DeleteButton_InvokesOnDelete — Glossary delete is IMMEDIATE (no confirm dialog, unlike
        // Calculations). Driving an actual delete needs a test-owned term, which is impossible because
        // glossary CREATE is broken server-side (HTTP 500 SQL 2601 — the Name column is inserted empty, so
        // the second create collides on UX_GlossaryTerm_Name_Current; see CreateTermFailsWithErrorBanner).
        // We therefore assert the STRUCTURE: every rendered card exposes an inline "Delete" button and NO
        // confirm dialog exists in the markup. We never click Delete (would destroy ambient rows).
        var (ctx, page) = await OpenAsync();
        try
        {
            var cards = page.Locator(CardSelector);
            if (await cards.CountAsync() == 0)
            {
                await Assertions.Expect(page.GetByText(new Regex("No glossary terms defined", RegexOptions.IgnoreCase)))
                    .ToBeVisibleAsync(new() { Timeout = 8_000 });
                return; // seed blocker documented — nothing to assert against
            }
            // Each card carries an inline Delete button; there is no "Confirm Delete" dialog anywhere.
            await Assertions.Expect(cards.First.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToBeVisibleAsync(new() { Timeout = 8_000 });
            (await page.GetByText(new Regex("Confirm Delete", RegexOptions.IgnoreCase)).CountAsync()).ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task NewTermTogglesCreatePanel()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // Branch: _showCreate toggles the inline create panel with Term + Definition inputs.
            await Assertions.Expect(page.GetByPlaceholder("Term name")).Not.ToBeVisibleAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "New Term" }).ClickAsync();
            await Assertions.Expect(page.GetByPlaceholder("Term name")).ToBeVisibleAsync(new() { Timeout = 8_000 });
            await Assertions.Expect(page.GetByPlaceholder("Term definition")).ToBeVisibleAsync();
            // Cancel closes it again.
            await page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
            await Assertions.Expect(page.GetByPlaceholder("Term name")).Not.ToBeVisibleAsync(new() { Timeout = 8_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task BlankTermCreateIsClientNoOp()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // Branch (Glossary/Index.razor:124): SubmitCreate guards `if (IsNullOrWhiteSpace(_createTerm)) return;`
            // → clicking Create with a blank Term is a silent no-op: NO error banner, panel STAYS OPEN, and
            // no POST is issued. This is correct client behavior and asserted green.
            await page.GetByRole(AriaRole.Button, new() { Name = "New Term" }).ClickAsync();
            await Assertions.Expect(page.GetByPlaceholder("Term name")).ToBeVisibleAsync(new() { Timeout = 8_000 });
            // Leave Term blank, optionally fill only Definition.
            await page.GetByPlaceholder("Term definition").FillAsync("definition with no term");
            await page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();
            await page.WaitForTimeoutAsync(1_500);
            // Panel still open (Term input still visible) and no error banner appeared.
            await Assertions.Expect(page.GetByPlaceholder("Term name")).ToBeVisibleAsync();
            (await page.Locator(".bg-red-950\\/30").CountAsync()).ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// RED — CONFIRMED APP BUG. Glossary search does not filter. Typing any query (even a random GUID that
    /// cannot match any term) and clicking "Search" leaves the FULL term list rendered — the same card count
    /// before and after, no empty-state card, no error banner. Observed live: 12 cards before, 12 after a
    /// no-match search, 12 after a matching-token search. The page's <c>OnSearch</c> →
    /// <c>SearchTerms(query)</c> path (<c>GET /catalog/glossary/search?q=</c>) returns the whole glossary
    /// regardless of <c>q</c> (the server-side search ignores the query parameter, or the provider returns
    /// all rows). This test asserts the CORRECT behavior (a no-match search empties the list to the
    /// "No glossary terms defined" empty state) and is therefore RED until the search is fixed server-side.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task SearchNoMatchShowsEmptyState()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // A GUID query cannot match any persisted term → server search SHOULD return nothing → empty card.
            await page.GetByPlaceholder(new Regex("Search glossary terms", RegexOptions.IgnoreCase))
                .FillAsync("zzz-no-such-term-" + Guid.NewGuid().ToString("N"));
            await page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("No glossary terms defined", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            (await page.Locator(CardSelector).CountAsync()).ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// RED — CONFIRMED APP BUG. Creating a glossary term through the UI fails: filling Term + Definition and
    /// clicking "Create" closes the panel but surfaces the red error banner "GlossaryProvider: Failed to
    /// create glossary term '{term}'" and the term never appears as a card. The provider's
    /// <c>POST /catalog/glossary</c> → the base <c>CreateGlossaryTermEndpoint</c> in
    /// Fdw.Services.Catalog.Endpoints (concrete: Reference.Api Endpoints/CatalogEndpoints.cs)
    /// returns HTTP 500 ("Failed to create glossary term"), which the UI maps to GlossaryProviderLog.CreateFailed.
    /// This test asserts the CORRECT behavior (the created term shows as a card with no error banner) and is
    /// therefore RED until the server-side create is fixed. It self-cleans via the UI Delete control on the
    /// off chance the create ever succeeds.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task CreateTermFailsWithErrorBanner()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var term = string.Concat("e2e-gl-", Guid.NewGuid().ToString("N").AsSpan(0, 8));
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "New Term" }).ClickAsync();
            await page.GetByPlaceholder("Term name").FillAsync(term);
            await page.GetByPlaceholder("Term definition").FillAsync("e2e definition");
            await page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();
            await page.WaitForTimeoutAsync(3_000);

            // CORRECT behavior: the new term appears as a card and NO error banner shows. With the bug, the
            // card never appears and the banner reads "Failed to create glossary term '{term}'" — RED.
            (await page.Locator(".bg-red-950\\/30").CountAsync()).ShouldBe(0);
            await Assertions.Expect(page.Locator(CardSelector).Filter(new() { HasTextString = term }).First)
                .ToBeVisibleAsync(new() { Timeout = 8_000 });
        }
        finally
        {
            // Self-clean: if the term did land, delete it via its inline Delete button (no confirm dialog).
            try
            {
                var card = page.Locator(CardSelector).Filter(new() { HasTextString = term });
                if (await card.CountAsync() > 0)
                {
                    await card.First.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
                    await page.WaitForTimeoutAsync(1_500);
                }
            }
            catch { /* best-effort cleanup */ }
            await ctx.CloseAsync();
        }
    }

    /// <summary>
    /// RED — CONFIRMED APP BUG. Every persisted glossary row renders with an EMPTY term name: the term
    /// <c>&lt;h3 class="...font-bold text-lg"&gt;&lt;/h3&gt;</c> is blank while the definition <c>&lt;p&gt;</c>
    /// carries text. On the live slot the leftover rows all show an empty h3 with definition "A test glossary
    /// term". This is downstream evidence that the create/persist path (or the GetGlossary DTO mapping) does
    /// not store/return the Term value. This test asserts the CORRECT behavior (a rendered term card has a
    /// non-empty term name) and is RED until the persistence/mapping is fixed. It self-skips (green) only if
    /// the slot genuinely has zero glossary rows, since there is then nothing to assert against.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task PersistedTermsHaveBlankTermName()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            var cards = page.Locator(CardSelector);
            var count = await cards.CountAsync();
            if (count == 0)
                return; // nothing persisted to evaluate — not the bug under test
            // CORRECT behavior: at least one rendered term card has a non-empty term name in its <h3>.
            var names = await page.Locator($"{CardSelector} h3").AllInnerTextsAsync();
            names.Any(n => !string.IsNullOrWhiteSpace(n)).ShouldBeTrue(
                "Every persisted glossary card rendered a blank <h3> term name — the Term value is not stored/returned.");
        }
        finally { await ctx.CloseAsync(); }
    }
}
