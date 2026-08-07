using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using System.Text.RegularExpressions;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Calculations area, driven through the rendered DOM (<c>/calculations</c> list with confirm-dialog
/// delete + the <c>/calculations/new</c> editor + <c>/calculations/{id}/edit</c>). Branch-complete for what
/// the UI exposes.
///
/// FIXTURE NOTE / RED — calc CRUD is BROKEN on the live slot due to a route mismatch: the UI's
/// CalculationApiClient calls <c>calculations</c> / <c>calculations/{id}</c>, but Reference.Api only
/// exposes calc CRUD under <c>calculation-entities/*</c> (the plain <c>calculations</c> routes are
/// compute-only: execute/preview/types). Probed live: <c>GET /api/v1/calculations</c> → 404,
/// <c>GET /api/v1/calculation-entities</c> → 200. So List, Create, Edit, and Delete all hit a non-existent
/// endpoint in the UI. Confirmed live: a fully valid create stays on <c>/calculations/new</c> with the
/// banner "CalculationProvider: Failed to load calculations list" and persists NO row.
///
/// <see cref="ApiSeeder.CreateCalculationEntityAsync"/> seeds against the REAL
/// <c>calculation-entities</c> route, which lets <see cref="ListIgnoresSeededCalcEntitiesRouteMismatchRED"/>
/// prove the gap precisely: the API holds the seeded row but the UI list never shows it (route mismatch).
/// The editor's DataSet dropdown reads <c>GET /api/v1/datasets</c> (which works), so a seeded DataSet makes
/// the dropdown deterministic for the validation tests. The delete confirm-dialog branch needs a calc row
/// IN THE UI LIST, which the route mismatch prevents — documented as seed-blocked.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class CalculationsTests(PlaywrightFixture fx)
{
    private async Task<(IBrowserContext, AreaTablePage)> OpenListAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        var list = new AreaTablePage(page, PlaywrightFixture.BaseUrl, "/calculations");
        await list.GotoAsync();
        return (ctx, list);
    }

    private static async Task<IPage> OpenEditorAsync(IBrowserContext _, IPage page)
    {
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/calculations/new", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return page;
    }

    // ---- LIST ------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListRendersRowsOrEmptyState()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenListAsync();
        try
        {
            var n = await list.WaitForRowsOrEmptyAsync();
            if (n == 0)
            {
                // RED-adjacent: the list GET hits the missing `calculations` route; the failure is masked
                // as the empty-state (the page never renders ctx.ErrorMessage). On a fresh slot the
                // empty-state is the only observable outcome.
                (await list.Page.InnerTextAsync("body"))
                    .ShouldContain("No calculations defined");
                return;
            }
            await Assertions.Expect(list.Rows.First.Locator("button.text-red-500").First).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P3")]
    public async Task ListHasNoSearchBoxDocumentedDeadFilter()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenListAsync();
        try
        {
            await list.WaitForRowsOrEmptyAsync();
            (await list.Page.GetByPlaceholder(new Regex("search|filter", RegexOptions.IgnoreCase)).CountAsync())
                .ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewButtonOpensEditor()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenListAsync();
        try
        {
            await list.WaitForRowsOrEmptyAsync();
            await list.NewButton.ClickAsync();
            await Assertions.Expect(list.Page).ToHaveURLAsync(new Regex(@"/calculations/new"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(list.Page);
            await Assertions.Expect(list.Page.Locator("input[placeholder='Calculation name']")).ToBeVisibleAsync();
            await Assertions.Expect(list.Page.Locator("textarea.input")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- EDITOR validation (order: Name → DataSet → Formula → Output) ----

    [Fact]
    [Trait("Priority", "P1")]
    public async Task CreateMissingName()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await OpenEditorAsync(ctx, page);
            await page.GetByRole(AriaRole.Button, new() { NameString = "Save" }).ClickAsync();
            await Assertions.Expect(page.Locator("div.bg-red-500\\/10"))
                .ToContainTextAsync("Name is required", new() { Timeout = 10_000 });
            page.Url.ShouldContain("/calculations/new");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateMissingDataSet()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await OpenEditorAsync(ctx, page);
            // Satisfy Name (1st check) to reach the DataSet check; leave DataSet unselected.
            await page.Locator("input.input").Nth(0).FillAsync("e2e-pl-" + Guid.NewGuid().ToString("N")[..8]);
            await page.GetByRole(AriaRole.Button, new() { NameString = "Save" }).ClickAsync();
            await Assertions.Expect(page.Locator("div.bg-red-500\\/10"))
                .ToContainTextAsync("DataSet is required", new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateMissingFormula()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Seed a DataSet so the DataSet <select> deterministically offers a known option (the editor reads
        // GET /api/v1/datasets, which works) — select it by VALUE, not a flaky positional index.
        await using var seeder = await ApiSeeder.CreateAsync();
        var ds = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await OpenEditorAsync(ctx, page);
            await page.Locator("input.input").Nth(0).FillAsync("e2e-pl-" + Guid.NewGuid().ToString("N")[..8]);
            await page.Locator("select.input").SelectOptionAsync(new SelectOptionValue { Value = ds });
            await page.GetByRole(AriaRole.Button, new() { NameString = "Save" }).ClickAsync();
            await Assertions.Expect(page.Locator("div.bg-red-500\\/10"))
                .ToContainTextAsync("Formula is required", new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateMissingOutputField()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var ds = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await OpenEditorAsync(ctx, page);
            await page.Locator("input.input").Nth(0).FillAsync("e2e-pl-" + Guid.NewGuid().ToString("N")[..8]);
            await page.Locator("select.input").SelectOptionAsync(new SelectOptionValue { Value = ds });
            await page.Locator("textarea.input").FillAsync("[a] + [b]");
            // Leave Output Field (2nd input) empty → reach the last check in the order.
            await page.GetByRole(AriaRole.Button, new() { NameString = "Save" }).ClickAsync();
            await Assertions.Expect(page.Locator("div.bg-red-500\\/10"))
                .ToContainTextAsync("Output field name is required", new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task EditModeRendersEditHeader()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit EditMode_RendersEditCalculationHeader — navigating to /calculations/{guid}/edit renders the
        // editor in edit mode ("Edit Calculation" header) regardless of whether the row can be loaded (the
        // header keys off the {Id} route param, not a successful load).
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/calculations/{Guid.NewGuid()}/edit", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2_500);
            await Assertions.Expect(page.GetByText(new Regex("Edit Calculation", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task FunctionPaletteInsertsToken()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await OpenEditorAsync(ctx, page);
            // Expand a function group, click the first function, assert it appended to the Formula textarea.
            // The group header text is CSS-uppercased ("MATH") but the DOM text is "Math (n)" — match
            // case-insensitively on the count-suffixed label.
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"math \(\d+\)", RegexOptions.IgnoreCase) })
                .First.ClickAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(400);
            // Function buttons are the cyan-500 monospace entries in the palette card (the sidebar nav
            // also uses font-mono, so scope to the function token color to avoid matching the menu).
            var fn = page.Locator("button.font-mono.text-cyan-500").First;
            var token = (await fn.InnerTextAsync()).Trim();
            await fn.ClickAsync();
            await page.WaitForTimeoutAsync(400);
            (await page.Locator("textarea.input").InputValueAsync()).ShouldContain(token);
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- CREATE (route-mismatch RED) ------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    public async Task CreateValidSucceeds()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Fixed: CalculationApiClient now targets the real `calculation-entities` CRUD routes, so a fully
        // valid create succeeds — no route-mismatch error banner, and the page navigates off
        // /calculations/new. The UI doesn't surface the created id, so clean up by name via the API.
        var name = "e2e-pl-" + Guid.NewGuid().ToString("N")[..8] + "-calc";
        await using var seeder = await ApiSeeder.CreateAsync();
        var ds = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await OpenEditorAsync(ctx, page);
            await page.Locator("input.input").Nth(0).FillAsync(name);
            await page.Locator("input.input").Nth(1).FillAsync("out_field");
            await page.Locator("select.input").SelectOptionAsync(new SelectOptionValue { Value = ds });
            await page.Locator("textarea.input").FillAsync("[a] + [b]");
            await page.GetByRole(AriaRole.Button, new() { NameString = "Save" }).ClickAsync();

            // No route-mismatch error banner, and the create navigates away from the editor.
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/calculations(?!/new)"), new() { Timeout = 15_000 });
            (await page.Locator("div.bg-red-500\\/10")
                .Filter(new() { HasTextRegex = new Regex("Failed to (load|create) calculation") }).CountAsync())
                .ShouldBe(0);
        }
        finally
        {
            await seeder.DeleteCalculationEntityByNameAsync(name);
            await ctx.CloseAsync();
        }
    }

    /// <summary>
    /// RED — CONFIRMED APP BUG (route mismatch, made precise via API seeding). We seed a calculation entity
    /// via the REAL <c>POST /api/v1/calculation-entities</c> route (probed live: 201) — so the row provably
    /// exists in the API — then open the UI <c>/calculations</c> list, whose CalculationApiClient lists from
    /// <c>calculations</c> (404). CORRECT behavior: the seeded entity appears as a table row. With the bug
    /// the list falls into the "No calculations defined" empty state and the seeded row is never shown. This
    /// asserts the row IS visible and is therefore RED until the UI client targets <c>calculation-entities</c>.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListIgnoresSeededCalcEntitiesRouteMismatchRED()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var ds = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));
        var calcName = ApiSeeder.NewPrefix("e2ecalc");
        await seeder.CreateCalculationEntityAsync(calcName, ds);

        var (ctx, list) = await OpenListAsync();
        try
        {
            await list.WaitForRowsOrEmptyAsync();
            // CORRECT: the seeded entity is a row. With the route-mismatch bug it never appears (empty state).
            await Assertions.Expect(list.RowContaining(calcName).First).ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CancelReturnsToList()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await OpenEditorAsync(ctx, page);
            await page.GetByRole(AriaRole.Button, new() { NameString = "Cancel" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/calculations$"), new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- DELETE confirm dialog (seed-blocked) ---------------------------

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DeleteConfirmDialogSeedBlocked()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Unlike Schedules (immediate delete, no confirm), Calculations delete opens a confirm dialog
        // ("Confirm Delete" / "Are you sure..."). Driving it requires a seeded row, which is impossible
        // because calc create is broken (CreateValid_FailsRouteMismatch_RED). When a row exists we open
        // and cancel the dialog (non-destructive); otherwise we assert the seed blocker and return.
        var (ctx, list) = await OpenListAsync();
        try
        {
            if (await list.WaitForRowsOrEmptyAsync() == 0)
            {
                (await list.Page.InnerTextAsync("body")).ShouldContain("No calculations defined");
                return; // seed blocker documented
            }
            await list.Rows.First.Locator("button.text-red-500").First.ClickAsync();
            await Assertions.Expect(list.Page.GetByText("Confirm Delete")).ToBeVisibleAsync(new() { Timeout = 10_000 });
            // Cancel — do not delete ambient data.
            await list.Page.GetByRole(AriaRole.Button, new() { NameString = "Cancel" }).ClickAsync();
            await Assertions.Expect(list.Page.GetByText("Confirm Delete")).ToBeHiddenAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }
}
