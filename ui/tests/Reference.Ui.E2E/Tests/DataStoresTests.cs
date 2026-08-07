using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// DataStores area, driven through the rendered browser DOM. The list is an HTML table
/// (<c>.table tbody tr</c>, name in a bold <c>td</c>, per-row pencil + trash buttons). There is NO
/// search box on this page (the provider supports filtering but the page renders none). Covers list
/// (exact seeded rows, empty-state is not assertable on a shared slot), the 3-step register wizard
/// (Step gates + valid create), edit-persistence, delete confirm/cancel, and the drill-down detail.
/// Fixtures are seeded via <see cref="ApiSeeder"/> (bound to the real <c>OpsDb</c> connection) and
/// self-clean in <c>finally</c>/Dispose.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class DataStoresTests(PlaywrightFixture fx)
{
    // Each datastore is a table row; the name is a bold td.
    private const string RowSelector = ".table tbody tr";

    private static System.Text.RegularExpressions.Regex Ci(string p) =>
        new(p, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private async Task<(IBrowserContext, IPage)> GotoListAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datastores", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    private static ILocator RowContaining(IPage page, string text) =>
        page.Locator(RowSelector).Filter(new() { HasTextString = text });

    /// <summary>
    /// Selects a connection (by its <c>conn.Name</c> option value) in the DataStore wizard Step-0
    /// connection &lt;select&gt;. That select only renders after connections load async, so poll its
    /// option values until the target is present before selecting.
    /// </summary>
    private static async Task SelectConnectionAsync(IPage page, string connectionName, int timeoutMs = 25_000)
    {
        var select = page.Locator("select").First;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var values = await select.Locator("option").EvaluateAllAsync<string[]>("els=>els.map(e=>e.value).filter(v=>v)");
            if (values.Contains(connectionName, StringComparer.Ordinal))
            {
                await select.SelectOptionAsync(new SelectOptionValue { Value = connectionName });
                return;
            }
            await page.WaitForTimeoutAsync(500);
        }
        throw new Xunit.Sdk.XunitException($"Connection '{connectionName}' never appeared in the wizard connection <select>.");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListRendersTableWithSeededRows()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var prefix = ApiSeeder.NewPrefix("e2e-dslist");
        var a = await seeder.CreateDataStoreAsync($"{prefix}-alpha", "OpsDb", description: "DS alpha");
        var b = await seeder.CreateDataStoreAsync($"{prefix}-bravo", "OpsDb", description: "DS bravo");

        var (ctx, page) = await GotoListAsync();
        try
        {
            await page.Locator(RowSelector).First.WaitForAsync(new() { Timeout = 15_000 });
            // The table shows column headers and exactly my 2 seeded rows (scoped by prefix).
            (await page.InnerTextAsync("body")).ShouldContain("Connection");
            await Assertions.Expect(RowContaining(page, a)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            await Assertions.Expect(RowContaining(page, b)).ToHaveCountAsync(1);
            // Each seeded row shows its bound connection name (OpsDb).
            (await RowContaining(page, a).InnerTextAsync()).ShouldContain("OpsDb");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RegisterStoreButtonOpensWizard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await GotoListAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("register store") }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(Ci("/datastores/new"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
            // Step 0 fields render (Name input).
            await Assertions.Expect(page.GetByPlaceholder(Ci("PROD_SALES"))).ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task WizardStep0NextGatedOnNameAndConnection()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datastores/new", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var nameInput = page.GetByPlaceholder(Ci("PROD_SALES"));
            await nameInput.WaitForAsync(new() { Timeout = 15_000 });
            var next = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^next$") });
            // Nothing filled → disabled.
            await Assertions.Expect(next).ToBeDisabledAsync();
            // Name only, no connection → still disabled.
            await nameInput.FillAsync(ApiSeeder.NewPrefix("e2e-dsgate"));
            await nameInput.PressAsync("Tab"); // @bind commits on blur
            await Assertions.Expect(next).ToBeDisabledAsync();
            // Pick a connection → enabled.
            var connSelect = page.Locator("select").First;
            var connValues = await connSelect.Locator("option").EvaluateAllAsync<string[]>("els=>els.map(e=>e.value).filter(v=>v)");
            if (connValues.Length == 0) return; // no connections to bind
            await connSelect.SelectOptionAsync(new SelectOptionValue { Value = connValues[0] });
            await page.WaitForTimeoutAsync(800); // OnConnectionChanged loads capabilities
            await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Bug", "RUI-datastore-wizard-storetype-step-never-advances")]
    public async Task WizardFullValidCreatesDataStore()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // CORRECT behavior asserted: Basic Info → Store Type → Container Management → Create lands a new
        // row. Step 0 advances cleanly. RED — CONFIRMED BUG: on Step 1 the Store_Type list never yields a
        // selection that enables Next — the store-type/capabilities load for the bound connection does not
        // populate a usable StoreType, so `CanAdvance(step1)` (StoreType non-empty) is never satisfied and
        // the wizard cannot reach the Create step. (Step-0 gating + the seeded-API create path are green.)
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-dsnew");
        seeder.TrackDataStoreForCleanup(name);

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datastores/new", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            // Fill Name + commit first, then select the first available connection (mirrors the proven
            // gated test). The connection's onchange sets ConnectionName and re-renders Next.
            var dsName = page.GetByPlaceholder(Ci("PROD_SALES"));
            await dsName.WaitForAsync(new() { Timeout = 15_000 });
            await dsName.FillAsync(name);
            await dsName.PressAsync("Tab"); // @bind commits on blur
            var connSelect = page.Locator("select").First;
            var connValues = await connSelect.Locator("option").EvaluateAllAsync<string[]>("els=>els.map(e=>e.value).filter(v=>v)");
            if (connValues.Length == 0) { Assert.Skip("No connections available to bind on this slot."); return; }
            await connSelect.SelectOptionAsync(new SelectOptionValue { Value = connValues[0] });
            await page.WaitForTimeoutAsync(1500); // capabilities load + re-render
            var next = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^next$") });
            await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 15_000 });
            await next.ClickAsync();

            // Step 1 — Store Type. The store-type list loads async after the connection's capabilities;
            // poll the dropdown until it has concrete options, else use the manual input fallback.
            var storeSelect = page.Locator("select").First;
            var storeValues = Array.Empty<string>();
            var stDeadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < stDeadline)
            {
                storeValues = await storeSelect.Locator("option").EvaluateAllAsync<string[]>("els=>els.map(e=>e.value).filter(v=>v)");
                if (storeValues.Length > 0) break;
                await page.WaitForTimeoutAsync(500);
            }
            if (storeValues.Length > 0)
            {
                await storeSelect.SelectOptionAsync(new SelectOptionValue { Value = storeValues[0] });
            }
            else
            {
                // Fallback manual store-type input (rendered when types could not be loaded).
                var manual = page.GetByPlaceholder(Ci("SqlServer"));
                if (await manual.CountAsync() == 0) { Assert.Skip("No store-type control available on this slot."); return; }
                await manual.FillAsync("MsSql");
                await manual.PressAsync("Tab");
            }
            await page.WaitForTimeoutAsync(800);
            await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 15_000 });
            await next.ClickAsync();

            // Step 2 — Containers (optional). Create directly.
            var create = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("create datastore") });
            await Assertions.Expect(create).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await create.ClickAsync();

            // Success → navigation back to /datastores, new row present.
            await Assertions.Expect(page).ToHaveURLAsync(Ci("/datastores$"), new() { Timeout = 30_000 });
            await Assertions.Expect(RowContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task WizardStep1NextGatedOnStoreType()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datastores/new", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var stName = page.GetByPlaceholder(Ci("PROD_SALES"));
            await stName.FillAsync(ApiSeeder.NewPrefix("e2e-dsst"));
            await stName.PressAsync("Tab"); // @bind commits on blur
            await SelectConnectionAsync(page, "OpsDb");
            await page.WaitForTimeoutAsync(1200);
            var next = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^next$") });
            await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 10_000 });
            await next.ClickAsync();
            // Step 1: no store type chosen yet → Next disabled.
            await Assertions.Expect(next).ToBeDisabledAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Bug", "RUI-datastore-wizard-storetype-step-never-advances")]
    public async Task EditPersistsDescriptionAndDisplayName()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // CORRECT behavior asserted: edit DisplayName + Description, walk to the final step, save, re-open
        // and read the persisted values back. RED — same confirmed bug as Wizard_FullValid: the edit
        // wizard cannot advance past the Store Type step, so "Save Changes" on the final step is
        // unreachable. (Field @bind + the API PUT both work — verified by ApiSeeder round-trips.)
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataStoreAsync(ApiSeeder.NewPrefix("e2e-dsedit"), "OpsDb",
            description: "orig desc", displayName: "Orig Display");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datastores/{name}/edit", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldNotShowErrorAsync(page);
            var displayInput = page.GetByPlaceholder(Ci("friendly name"));
            await displayInput.WaitForAsync(new() { Timeout = 15_000 });
            await displayInput.FillAsync("Edited Display");
            await displayInput.PressAsync("Tab"); // @bind commits on blur
            var descArea = page.Locator("textarea").First;
            await descArea.FillAsync("edited description");
            await descArea.PressAsync("Tab");

            // The editor is a 3-step wizard even in edit mode; "Save Changes" lives on the final step.
            // StoreType is preloaded from the existing store, so Next is enabled on each step.
            var next = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^next$") });
            await next.ClickAsync();
            await page.WaitForTimeoutAsync(600);
            await next.ClickAsync();
            await page.WaitForTimeoutAsync(600);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("save changes") }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(Ci("/datastores$"), new() { Timeout = 20_000 });

            // Re-open edit and read back (step 0 holds DisplayName + Description).
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datastores/{name}/edit", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.GetByPlaceholder(Ci("friendly name")).WaitForAsync(new() { Timeout = 15_000 });
            (await page.GetByPlaceholder(Ci("friendly name")).InputValueAsync()).ShouldBe("Edited Display");
            (await page.Locator("textarea").First.InputValueAsync()).ShouldBe("edited description");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task EditNameInputIsReadOnly()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataStoreAsync(ApiSeeder.NewPrefix("e2e-dsro"), "OpsDb");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datastores/{name}/edit", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var nameInput = page.GetByPlaceholder(Ci("PROD_SALES"));
            await nameInput.WaitForAsync(new() { Timeout = 15_000 });
            // Name is the route key → rendered disabled in edit mode.
            await Assertions.Expect(nameInput).ToBeDisabledAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeleteConfirmModalConfirmRemovesRow()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataStoreAsync(ApiSeeder.NewPrefix("e2e-dsdel"), "OpsDb");

        var (ctx, page) = await GotoListAsync();
        try
        {
            await Assertions.Expect(RowContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            // Trash button (red icon) on the row → confirm modal.
            await RowContaining(page, name).Locator("button.text-red-500").Last.ClickAsync();
            await Assertions.Expect(page.GetByText(Ci("Confirm Delete"))).ToBeVisibleAsync();
            await page.Locator(".fixed").GetByRole(AriaRole.Button, new() { NameRegex = Ci("^delete$") }).ClickAsync();
            await Assertions.Expect(RowContaining(page, name)).ToHaveCountAsync(0, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeleteConfirmModalCancelKeepsRow()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataStoreAsync(ApiSeeder.NewPrefix("e2e-dskeep"), "OpsDb");

        var (ctx, page) = await GotoListAsync();
        try
        {
            await Assertions.Expect(RowContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            await RowContaining(page, name).Locator("button.text-red-500").Last.ClickAsync();
            await Assertions.Expect(page.GetByText(Ci("Confirm Delete"))).ToBeVisibleAsync();
            await page.Locator(".fixed").GetByRole(AriaRole.Button, new() { NameRegex = Ci("^cancel$") }).ClickAsync();
            await Assertions.Expect(page.GetByText(Ci("Confirm Delete"))).ToHaveCountAsync(0);
            (await RowContaining(page, name).CountAsync()).ShouldBe(1);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RowClickOpensDetailWithOverview()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataStoreAsync(ApiSeeder.NewPrefix("e2e-dsdetail"), "OpsDb",
            description: "detail desc");

        var (ctx, page) = await GotoListAsync();
        try
        {
            await Assertions.Expect(RowContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            await RowContaining(page, name).First.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(Ci($"/datastores/{System.Text.RegularExpressions.Regex.Escape(name)}$"),
                new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
            // Detail content loads async — wait for the Structure panel (detail-only) to paint.
            await Assertions.Expect(page.GetByText(Ci("Structure")).First).ToBeVisibleAsync(new() { Timeout = 20_000 });
            var body = await page.InnerTextAsync("body");
            // Overview panel shows the store's name, its connection, and the Structure/Actions panels.
            body.ShouldContain(name);
            body.ShouldContain("OpsDb");
            body.ShouldContain("Actions");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DetailEmptyStoreShowsImportHint()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataStoreAsync(ApiSeeder.NewPrefix("e2e-dsempty"), "OpsDb");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datastores/{name}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldNotShowErrorAsync(page);
            // A store with zero paths shows the empty-tree import hint.
            await Assertions.Expect(page.GetByText(Ci("No paths discovered|Import schema to populate")))
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }
}
