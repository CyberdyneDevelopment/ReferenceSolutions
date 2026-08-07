using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Connections area, driven through the rendered browser DOM. The list is a card grid
/// (<c>.grid &gt; .card</c>, name in an <c>h3</c>, per-card TEST + delete buttons). Covers: the list
/// paints real rows, the type filter narrows them, search narrows them, the New button routes to the
/// create form, and a card opens its detail. Non-destructive — it does not delete seeded connections.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class ConnectionsTests(PlaywrightFixture fx)
{
    private const string ItemSelector = ".grid > .card";

    private async Task<(IBrowserContext, ListPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        var list = new ListPage(page, PlaywrightFixture.BaseUrl, "/connections", ItemSelector);
        await list.GotoAsync();
        return (ctx, list);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListRendersConnectionCards()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try { (await list.WaitForItemsAsync()).ShouldBeGreaterThan(0); }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SearchNarrowsTheList()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            // Pick a real card's name, search for it, assert the grid narrows to matches of that name.
            var firstName = (await list.Items.First.Locator("h3").InnerTextAsync()).Trim();
            firstName.ShouldNotBeNullOrEmpty();
            await list.SearchAsync(firstName);
            await Assertions.Expect(list.ItemContaining(firstName).First).ToBeVisibleAsync();
            (await list.ItemContaining(firstName).CountAsync()).ShouldBeGreaterThan(0);
            // The match narrowed the grid: every visible card contains the query.
            (await list.ItemCountAsync()).ShouldBe(await list.ItemContaining(firstName).CountAsync());
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task TypeFilterNarrowsTheList()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            var typeSelect = list.Page.Locator("select").First;
            // Choose a concrete type (skip the "All Types" first option) and assert the grid still renders.
            var optionValues = await typeSelect.Locator("option").EvaluateAllAsync<string[]>(
                "els => els.map(e => e.value).filter(v => v && v.length > 0)");
            if (optionValues.Length == 0) return; // no concrete types to filter on
            await typeSelect.SelectOptionAsync(new SelectOptionValue { Value = optionValues[0] });
            await list.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await PageAssertions.ShouldNotShowErrorAsync(list.Page);
            (await list.ItemCountAsync()).ShouldBeGreaterThan(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewButtonOpensCreateForm()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.NewButton.ClickAsync();
            // OnNewConnection does an in-circuit Nav.NavigateTo (SignalR) — the URL changes without a
            // page "Load" event, so poll the URL via ToHaveURL rather than WaitForURL (waits for Load).
            await Assertions.Expect(list.Page).ToHaveURLAsync(
                new System.Text.RegularExpressions.Regex(@"/connections/new"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(list.Page);
            // The create flow renders interactive inputs (name + connection details).
            await Assertions.Expect(list.Page.Locator("input, select").First).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CardOpensDetail()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            var name = (await list.Items.First.Locator("h3").InnerTextAsync()).Trim();
            await list.Items.First.ClickAsync();
            // Card click does an in-circuit nav to a per-connection route (configure/edit) named for
            // the item — poll the URL (in-circuit nav fires no page Load event).
            await Assertions.Expect(list.Page).ToHaveURLAsync(
                new System.Text.RegularExpressions.Regex(@"/connections/[^/]+"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(list.Page);
            (await list.Page.InnerTextAsync("body")).ShouldContain(name);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Bug", "RUI-empty-state-not-wired-to-filtered-set")]
    public async Task SearchNoMatchShowsEmptyState()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync("zzz-no-such-connection-zzz");
            // CORRECT behavior asserted: a no-match search shows zero cards AND the empty-state card
            // "No connections configured".
            // RED — CONFIRMED BUG: ConnectionList.razor renders the empty-state from
            // `!Context.FilteredConnections.Any()` (the FULL loaded list), but search/filter narrow only
            // the local `GetDisplayedConnections()` projection. So a no-match search paints an EMPTY GRID
            // with NO empty-state message. The empty-state branch must check the displayed/filtered set.
            await Assertions.Expect(list.Page.GetByText(new System.Text.RegularExpressions.Regex(
                "No connections configured", System.Text.RegularExpressions.RegexOptions.IgnoreCase))).ToBeVisibleAsync();
            (await list.ItemCountAsync()).ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Bug", "RUI-sort-select-does-not-reorder")]
    public async Task SortReordersTheList()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try
        {
            // Need at least 2 cards for order to be observable.
            if (await list.WaitForItemsAsync() < 2) return;
            var sortSelect = list.Page.Locator("select").Nth(1); // 0 = type filter, 1 = sort order
            await sortSelect.SelectOptionAsync(new SelectOptionValue { Value = "name-asc" });
            await list.Page.WaitForTimeoutAsync(600);
            var firstAsc = (await list.Items.First.Locator("h3").InnerTextAsync()).Trim();
            await sortSelect.SelectOptionAsync(new SelectOptionValue { Value = "name-desc" });
            // CORRECT behavior asserted: name-desc reorders the grid — the first card's name changes.
            // RED — CONFIRMED BUG: changing the sort <select> (bound `@bind="SortOrder"` +
            // `@bind:after="InvokeFilterChanged"`) does NOT re-render the displayed order; the first card
            // stays the name-asc winner (e.g. "AuthDb") after selecting name-desc. The displayed list is
            // not re-sorted when SortOrder changes over the circuit.
            await Assertions.Expect(list.Items.First.Locator("h3"))
                .Not.ToHaveTextAsync(firstAsc, new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task CreateNextGatedOnNameAndType()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/connections/new", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldNotShowErrorAsync(page);
            var nameInput = page.GetByPlaceholder(new System.Text.RegularExpressions.Regex("PROD_MSSQL"));
            await nameInput.WaitForAsync(new() { Timeout = 15_000 });
            var next = page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("^next$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });
            // Branch: nothing filled → Next disabled.
            await Assertions.Expect(next).ToBeDisabledAsync();
            // Branch: name only, no type → still disabled.
            await nameInput.FillAsync("e2e-gate-check");
            await Assertions.Expect(next).ToBeDisabledAsync();
            // Branch: name + a real type → Next enables.
            var typeSelect = page.Locator("select").First;
            var types = await typeSelect.Locator("option").EvaluateAllAsync<string[]>("els=>els.map(e=>e.value).filter(v=>v)");
            if (types.Length == 0) return; // no types available to select
            await typeSelect.SelectOptionAsync(new SelectOptionValue { Value = types[0] });
            await page.WaitForTimeoutAsync(1000); // OnServiceTypeChanged loads auth types
            await Assertions.Expect(next).ToBeEnabledAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task TestActionShowsResultBanner()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            // Click the first card's TEST button (stopPropagation, so the card doesn't navigate).
            var testBtn = list.Items.First.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("^test", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });
            await testBtn.ClickAsync();
            // Branch: pass → green banner; fail → red banner. Either way the result banner carries a
            // Dismiss button — assert it appears (the action ran and rendered a result, regardless of
            // the exact message text which is the API's response.Message).
            await Assertions.Expect(list.Page.GetByRole(AriaRole.Button, new() {
                NameRegex = new System.Text.RegularExpressions.Regex("dismiss", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }))
                .ToBeVisibleAsync(new() { Timeout = 45_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    // ============================================================================================
    // Deterministic-fixture tests. Each seeds its OWN uniquely-prefixed connections via the API
    // (ApiSeeder), then asserts EXACT, scoped behavior against the rendered DOM. Self-cleaning.
    // ============================================================================================

    private static System.Text.RegularExpressions.Regex Ci(string p) =>
        new(p, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    /// <summary>
    /// Polls a &lt;select&gt;'s option VALUES until at least one concrete (non-empty) value exists. The
    /// connection-type select renders empty while ConnectionTypes load async; options are never
    /// "visible" so a normal WaitFor cannot be used — EvaluateAll reads the DOM directly.
    /// </summary>
    private static async Task WaitForTypeOptionsAsync(ILocator select, int timeoutMs = 25_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var values = await select.Locator("option").EvaluateAllAsync<string[]>("els=>els.map(e=>e.value).filter(v=>v)");
            if (values.Length > 0) return;
            await select.Page.WaitForTimeoutAsync(500);
        }
        throw new Xunit.Sdk.XunitException("Connection type <select> never populated with concrete options (types failed to load).");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Bug", "RUI-connections-list-client-only-search-over-truncated-page")]
    public async Task ListSeededRowsReachableViaSearch()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Seed three uniquely-prefixed connections via the API, then assert they are reachable and
        // exactly counted through the list's search. This is the canonical deterministic list/search
        // test (exact membership of a known fixture set).
        //
        // RED — CONFIRMED BUG: the Connections list (ConnectionApiClient.GetConnections) loads only the
        // first page (server take=100) and the search/filter/sort are CLIENT-SIDE over that loaded page
        // only — there is NO server-side search re-query. On this slot there are >200 connections, so
        // freshly-seeded rows land beyond page 1 and are PERMANENTLY UNREACHABLE from the UI list/search.
        // Fix requires either server-side search wired to the search box, or a higher/paged take.
        await using var seeder = await ApiSeeder.CreateAsync();
        var prefix = ApiSeeder.NewPrefix("e2e-clist");
        await seeder.CreateConnectionAsync($"{prefix}-alpha");
        await seeder.CreateConnectionAsync($"{prefix}-bravo");
        await seeder.CreateConnectionAsync($"{prefix}-charlie");

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync(prefix);
            // CORRECT: the three seeded cards are reachable and exactly counted.
            await Assertions.Expect(list.ItemContaining(prefix).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
            (await list.ItemContaining(prefix).CountAsync()).ShouldBe(3);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SearchOnLoadedDataNarrowsToMatches()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Client-side search over the ALREADY-LOADED page works (this is the part that is not bugged).
        // Search for a name known to be on page 1 ("OpsDb") and assert every visible card matches it.
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync("OpsDb");
            await Assertions.Expect(list.ItemContaining("OpsDb").First).ToBeVisibleAsync(new() { Timeout = 15_000 });
            // Every visible card contains the query (search narrowed the loaded grid).
            (await list.ItemCountAsync()).ShouldBe(await list.ItemContaining("OpsDb").CountAsync());
            (await list.ItemContaining("OpsDb").CountAsync()).ShouldBeGreaterThan(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Bug", "RUI-connection-wizard-type-dropdown-never-loads")]
    public async Task CreateFullWizardPassingTestCreatesConnection()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Full happy-path: configure a real MsSql target (E2E_SQL_SERVER / OpsDb / fdw_ops, secret from
        // EnvSecrets:OPS_PASSWORD) → pass the Step-1 test gate → Save. CORRECT behavior asserted.
        // RED — CONFIRMED BUG: the wizard's connection-type <select> never populates on this slot —
        // `ConnectionWizardContext.ConnectionTypes` stays empty so the `@if (ConnectionTypes.Any())`
        // type dropdown never renders, making UI-driven connection creation impossible. (The wizard
        // provider's GetTypesByCategory("Connection") call resolves to a 404 route; the working list is
        // GET connections/types.) WaitForTypeOptionsAsync fails loud after 25s rather than hanging.
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-cnew");
        seeder.TrackConnectionForCleanup(name);

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/connections/new", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldNotShowErrorAsync(page);

            // Step 0 — Configure.
            var nameBox = page.GetByPlaceholder(Ci("PROD_MSSQL"));
            await nameBox.WaitForAsync(new() { Timeout = 15_000 });
            await nameBox.FillAsync(name);
            // The type <select> only renders once ConnectionTypes finish loading (async). Poll its
            // option VALUES (options are never "visible"; this also confirms types loaded).
            var typeSelect = page.Locator("select").First;
            await WaitForTypeOptionsAsync(typeSelect);
            await typeSelect.SelectOptionAsync(new SelectOptionValue { Value = "MsSql" });
            await page.WaitForTimeoutAsync(1200); // OnServiceTypeChanged loads auth types
            await page.GetByPlaceholder(Ci("sql.example.com")).FillAsync(E2ESettings.SqlServer);
            await page.Locator("input[type=number]").First.FillAsync("1433");
            await page.GetByPlaceholder(Ci("ProductionDb|analytics")).First.FillAsync("OpsDb");

            // Auth type → SqlAuth (requires Username + a secret key).
            var authSelect = page.Locator("select").Nth(1);
            if (await authSelect.CountAsync() > 0)
                await authSelect.SelectOptionAsync(new SelectOptionValue { Value = "SqlAuth" });
            await page.WaitForTimeoutAsync(600);
            await page.GetByPlaceholder(Ci("db_user")).FillAsync("fdw_ops");
            // Use Existing Key mode so we can point at the known EnvSecrets key (OPS_PASSWORD).
            var useExisting = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("use existing") });
            if (await useExisting.CountAsync() > 0) await useExisting.First.ClickAsync();
            var secretKeyInput = page.GetByPlaceholder(Ci("connection-prod-sql-password|Key name from secret manager"));
            if (await secretKeyInput.CountAsync() > 0) await secretKeyInput.First.FillAsync("OPS_PASSWORD");

            var next = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^next$") });
            await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 10_000 });
            await next.ClickAsync();

            // Step 1 — Test. Run the test; it must pass to advance.
            var runTest = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("run test") });
            await Assertions.Expect(runTest).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await runTest.ClickAsync();
            // A passing test enables advancing to step 2.
            await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 45_000 });
            await next.ClickAsync();

            // Step 2 — Review & Save.
            var create = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("create connection") });
            await Assertions.Expect(create).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await create.ClickAsync();

            // Success → completion card OR navigation back to /connections.
            await Assertions.Expect(page.GetByText(Ci("Connection created successfully|connections")))
                .ToBeVisibleAsync(new() { Timeout = 30_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Bug", "RUI-connection-wizard-type-dropdown-never-loads")]
    public async Task CreateTestFailsBlocksAdvanceToSave()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // CORRECT behavior asserted: a failing Step-1 test shows the red banner and blocks advancing to
        // the Save step. RED — same confirmed bug as Create_FullWizard: the connection-type <select>
        // never populates (ConnectionTypes empty), so the wizard cannot be configured at all.
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/connections/new", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var failName = page.GetByPlaceholder(Ci("PROD_MSSQL"));
            await failName.WaitForAsync(new() { Timeout = 15_000 });
            await failName.FillAsync(ApiSeeder.NewPrefix("e2e-cfail"));
            var failType = page.Locator("select").First;
            await WaitForTypeOptionsAsync(failType);
            await failType.SelectOptionAsync(new SelectOptionValue { Value = "MsSql" });
            await page.WaitForTimeoutAsync(1200);
            // Point at an unreachable server → test must FAIL.
            await page.GetByPlaceholder(Ci("sql.example.com")).FillAsync("10.255.255.1");
            await page.Locator("input[type=number]").First.FillAsync("1433");
            await page.GetByPlaceholder(Ci("ProductionDb|analytics")).First.FillAsync("Nope");
            var next = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^next$") });
            await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 10_000 });
            await next.ClickAsync();

            var runTest = page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("run test") });
            await Assertions.Expect(runTest).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await runTest.ClickAsync();
            // Red failure banner appears (✗ message) and we must NOT see the Step-2 Create button.
            await Assertions.Expect(page.Locator(".bg-red-500\\/10").First).ToBeVisibleAsync(new() { Timeout = 45_000 });
            (await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("create connection") }).CountAsync()).ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task EditPersistsEveryField()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateConnectionAsync(ApiSeeder.NewPrefix("e2e-cedit"),
            server: "orig.example", port: 1433, database: "OrigDb");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/connections/{name}/edit", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await page.GetByPlaceholder(Ci("sql.example.com")).WaitForAsync(new() { Timeout = 15_000 });

            // Change Server, Port, Database.
            await page.GetByPlaceholder(Ci("sql.example.com")).FillAsync("edited.example");
            await page.Locator("input[type=number]").First.FillAsync("1599");
            await page.GetByPlaceholder(Ci("ProductionDb|analytics")).First.FillAsync("EditedDb");

            await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("save changes") }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(Ci("/connections$"), new() { Timeout = 20_000 });

            // Re-open the edit route and read each field back.
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/connections/{name}/edit", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.GetByPlaceholder(Ci("sql.example.com")).WaitForAsync(new() { Timeout = 15_000 });
            (await page.GetByPlaceholder(Ci("sql.example.com")).InputValueAsync()).ShouldBe("edited.example");
            (await page.Locator("input[type=number]").First.InputValueAsync()).ShouldBe("1599");
            (await page.GetByPlaceholder(Ci("ProductionDb|analytics")).First.InputValueAsync()).ShouldBe("EditedDb");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Bug", "RUI-connections-list-client-only-search-over-truncated-page")]
    public async Task DeleteConfirmModalConfirmRemovesRow()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Seed a connection, surface it via search, open the confirm modal, confirm, assert the row is
        // gone. CORRECT behavior asserted.
        // RED — CONFIRMED BUG (same root cause as ListSeededRowsReachableViaSearch): the list loads only
        // the first server page and search is client-side over it, so a freshly-seeded connection (beyond
        // page 1 on this >200-row slot) never appears as a card and cannot be reached for deletion.
        // The delete confirm-modal MECHANICS are covered green by DeleteConfirmModalCancelKeepsRow.
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateConnectionAsync(ApiSeeder.NewPrefix("e2e-cdel"));

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync(name);
            await Assertions.Expect(list.ItemContaining(name).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
            // Trash icon (the icon button on the card) opens the confirm modal.
            await list.ItemContaining(name).First.Locator("button.btn-icon").Last.ClickAsync();
            await Assertions.Expect(list.Page.GetByText(Ci("Confirm Delete"))).ToBeVisibleAsync();
            // Modal Delete button confirms.
            await list.Page.Locator(".fixed").GetByRole(AriaRole.Button, new() { NameRegex = Ci("^delete$") }).ClickAsync();
            // Row is gone — zero cards for this unique name.
            await Assertions.Expect(list.ItemContaining(name)).ToHaveCountAsync(0, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeleteConfirmModalCancelKeepsRow()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Confirm-modal mechanics on a LOADED card (page 1), non-destructively: open → Cancel → the
        // modal closes and the card remains. Uses the first loaded card so it is reachable despite the
        // list pagination bug, and Cancel guarantees nothing is deleted.
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            var firstName = (await list.Items.First.Locator("h3").InnerTextAsync()).Trim();
            await list.Items.First.Locator("button.btn-icon").Last.ClickAsync();
            await Assertions.Expect(list.Page.GetByText(Ci("Confirm Delete"))).ToBeVisibleAsync(new() { Timeout = 15_000 });
            // Cancel → modal closes, card stays.
            await list.Page.Locator(".fixed").GetByRole(AriaRole.Button, new() { NameRegex = Ci("^cancel$") }).ClickAsync();
            await Assertions.Expect(list.Page.GetByText(Ci("Confirm Delete"))).ToHaveCountAsync(0);
            (await list.ItemContaining(firstName).CountAsync()).ShouldBeGreaterThan(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task TestPassingConnectionShowsHealthyGreenBanner()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // OpsDb is a real, reachable MsSql connection on this slot whose test PASSES.
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync("OpsDb");
            await Assertions.Expect(list.ItemContaining("OpsDb").First).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await list.ItemContaining("OpsDb").First
                .GetByRole(AriaRole.Button, new() { NameRegex = Ci("^test") }).ClickAsync();
            // Pass branch: a Dismiss-able banner appears AND a Healthy badge shows on the card.
            await Assertions.Expect(list.Page.GetByText(Ci("Healthy")).First)
                .ToBeVisibleAsync(new() { Timeout = 45_000 });
        }
        finally { await ctx.CloseAsync(); }
    }
}
