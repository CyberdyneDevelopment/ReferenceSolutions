using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// DataSets area (/datasets, /datasets/new, /datasets/{name}, /datasets/{name}/edit,
/// /datasets/calculated/new), driven through the rendered browser DOM. The list is a divided-list card of
/// <c>button.group</c> row buttons; the row shows DisplayName/Name, an abbreviation badge, a category chip,
/// and a description. Search binds with Blazor <c>@oninput</c> (live) and matches
/// Name/DisplayName/Abbreviation/Category/Description; the category <c>&lt;select&gt;</c> binds
/// <c>@bind:after</c>.
///
/// DETERMINISTIC FIXTURES — the create/edit wizard (/datasets/new) crashes its Blazor circuit on connect
/// (confirmed app bug, see <see cref="WizardCircuitCrashesOnConnect"/>), so there is NO working UI path to
/// create or edit a DataSet. BUT the API <c>POST /api/v1/datasets</c> works (probed live: 201), so every
/// list/search/filter/detail test seeds its OWN uniquely-prefixed DataSets via <see cref="ApiSeeder"/> and
/// asserts EXACT counts/ordering scoped to that prefix — never "&gt;= 1" against ambient junk. Fixtures
/// self-clean in <c>finally</c>. The Annotations detail tab and the calculated designer
/// (/datasets/calculated/new) run working circuits and are covered against seeded data.
///
/// bUnit branch parity (DataSetsPageTests / DataSetDetailPageTests / DataSetWizardPageTests): every
/// UI-reachable branch is covered below; the few that are physically unreachable end-to-end (the wizard's
/// per-step internals, which die with the circuit) are documented as bUnit-only on the RED wizard test.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class DataSetsTests(PlaywrightFixture fx)
{
    private const string ItemSelector = "button.group";
    private static readonly string[] AnnotationTags = { "e2etag1" };
    private static readonly string[] UniqueAnnotationTags = { "e2euniquetag" };

    private async Task<(IBrowserContext, ListPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        var list = new ListPage(page, PlaywrightFixture.BaseUrl, "/datasets", ItemSelector);
        await list.GotoAsync();
        return (ctx, list);
    }

    // ---- LIST ------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListRendersSeededRows()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var a = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: "E2eList");
        var b = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: "E2eList");

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            // Both seeded rows render as row buttons (DisplayName blank → row shows Name).
            await Assertions.Expect(list.ItemContaining(a).First).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(list.ItemContaining(b).First).ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SearchNarrowsToExactSeededMatch()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var target = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: "E2eSearch");
        var other = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: "E2eSearch");

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            // The full 8-hex prefix is unique to exactly one seeded row → narrows to that single row.
            await list.SearchAsync(target);
            await Assertions.Expect(list.ItemContaining(target).First).ToBeVisibleAsync(new() { Timeout = 8_000 });
            await Assertions.Expect(list.ItemContaining(other).First).Not.ToBeVisibleAsync(new() { Timeout = 8_000 });
            (await list.ItemCountAsync()).ShouldBe(1);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SearchMatchesCategoryToken()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // The live @oninput filter matches Category too (DataSetProvider.GetFilteredDataSets). Seed two rows
        // sharing a unique category token and assert searching that token surfaces BOTH.
        await using var seeder = await ApiSeeder.CreateAsync();
        var cat = string.Concat("E2eCat", Guid.NewGuid().ToString("N").AsSpan(0, 6));
        var a = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: cat);
        var b = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: cat);

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync(cat);
            await Assertions.Expect(list.ItemContaining(a).First).ToBeVisibleAsync(new() { Timeout = 8_000 });
            await Assertions.Expect(list.ItemContaining(b).First).ToBeVisibleAsync(new() { Timeout = 8_000 });
            (await list.ItemCountAsync()).ShouldBe(2);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SearchNoMatchEmptiesTheList()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: "E2eNoMatch");

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync("zzz-no-such-dataset-" + Guid.NewGuid().ToString("N"));
            // Branch (inventory): a no-match search renders the card with header + ZERO data rows (the
            // "No DataSets configured" empty-state checks the UNFILTERED list, so it does NOT show).
            await Assertions.Expect(list.Items.First).Not.ToBeVisibleAsync(new() { Timeout = 8_000 });
            (await list.ItemCountAsync()).ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CategoryFilterNarrowsToExactSeededCategory()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Seed two rows in a unique category + one in a different unique category. The filter <select> is
        // built from distinct categories, so both seeded categories become options; filtering to the first
        // shows exactly the two, filtering to the second shows exactly the one.
        await using var seeder = await ApiSeeder.CreateAsync();
        var catA = string.Concat("E2eFilA", Guid.NewGuid().ToString("N").AsSpan(0, 6));
        var catB = string.Concat("E2eFilB", Guid.NewGuid().ToString("N").AsSpan(0, 6));
        var a1 = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: catA);
        var a2 = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: catA);
        var b1 = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: catB);

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            var categorySelect = list.Page.Locator("select").First;

            await categorySelect.SelectOptionAsync(new SelectOptionValue { Value = catA });
            await Assertions.Expect(list.ItemContaining(a1).First).ToBeVisibleAsync(new() { Timeout = 8_000 });
            await Assertions.Expect(list.ItemContaining(a2).First).ToBeVisibleAsync(new() { Timeout = 8_000 });
            await Assertions.Expect(list.ItemContaining(b1).First).Not.ToBeVisibleAsync(new() { Timeout = 8_000 });
            (await list.ItemCountAsync()).ShouldBe(2);

            await categorySelect.SelectOptionAsync(new SelectOptionValue { Value = catB });
            await Assertions.Expect(list.ItemContaining(b1).First).ToBeVisibleAsync(new() { Timeout = 8_000 });
            await Assertions.Expect(list.ItemContaining(a1).First).Not.ToBeVisibleAsync(new() { Timeout = 8_000 });
            (await list.ItemCountAsync()).ShouldBe(1);

            // Back to "All categories": every seeded row visible again (≥3 incl. ambient).
            await categorySelect.SelectOptionAsync(new SelectOptionValue { Value = "" });
            await Assertions.Expect(list.ItemContaining(a1).First).ToBeVisibleAsync(new() { Timeout = 8_000 });
            (await list.ItemCountAsync()).ShouldBeGreaterThanOrEqualTo(3);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task RowRendersCategoryChipWithSeededCategory()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // POSITIVE branch of bUnit RowHidesCategoryPillWhenCategoryBlank: a row WITH a category renders the
        // category chip (span.bg-gray-800/40) carrying the category text.
        //
        // bUnit-only (the negative branch): the "hide chip when category blank" path is UNREACHABLE
        // end-to-end — the create endpoint forces Category="Dataset" when none is supplied (probed live:
        // POST a DataSet with null category → GET returns category "Dataset"), so a category-less row can
        // never be produced through the API/UI. The blank-category branch is covered only by bUnit.
        await using var seeder = await ApiSeeder.CreateAsync();
        var cat = string.Concat("E2eChip", Guid.NewGuid().ToString("N").AsSpan(0, 6));
        var name = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: cat);

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync(name);
            var row = list.ItemContaining(name).First;
            await Assertions.Expect(row).ToBeVisibleAsync(new() { Timeout = 8_000 });
            // The category chip is present and carries the seeded category text.
            var chip = row.Locator("span.bg-gray-800\\/40");
            await Assertions.Expect(chip.First).ToBeVisibleAsync(new() { Timeout = 8_000 });
            (await chip.First.InnerTextAsync()).ShouldContain(cat);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewButtonRoutesToWizard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenAsync();
        try
        {
            await list.NewButton.ClickAsync();
            // "+ New" does an in-circuit Nav.NavigateTo — poll the URL (no page Load fires).
            await Assertions.Expect(list.Page).ToHaveURLAsync(new Regex(@"/datasets/new"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(list.Page);
            await Assertions.Expect(list.Page.GetByPlaceholder("DATA_SET_IDENTIFIER")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- DETAIL ----------------------------------------------------------

    [Fact]
    [Trait("Priority", "P2")]
    public async Task RowOpensDetailWithHeaderBadgeAndFieldsTab()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"), category: "E2eDetail");

        var (ctx, list) = await OpenAsync();
        try
        {
            await list.WaitForItemsAsync();
            await list.SearchAsync(name);
            await list.ItemContaining(name).First.ClickAsync();
            // Row @onclick is an in-circuit nav to /datasets/{name}.
            await Assertions.Expect(list.Page).ToHaveURLAsync(new Regex($"/datasets/{name}"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(list.Page);
            // Detail header (uppercased name) + Active badge + Fields table render. bUnit
            // RendersActiveBadgeAndTabsWhenLoaded.
            await Assertions.Expect(list.Page.Locator("h1").Filter(new() { HasTextRegex = new Regex(name, RegexOptions.IgnoreCase) }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(list.Page.Locator(".badge-success").First).ToBeVisibleAsync();
            // Tab buttons render (Fields/Sources/Lineage/Preview/Annotations).
            await Assertions.Expect(list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Fields", RegexOptions.IgnoreCase) }).First).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DetailSourcesTabEmptyState()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit SourcesTab_ShowsEmptyMessageWhenNoSources — a seeded DataSet has no sources, so the Sources
        // tab shows "No sources configured." plus the "Map Fields" action.
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/{name}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(2_000);
            await PageAssertions.ShouldNotShowErrorAsync(page);

            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Sources", RegexOptions.IgnoreCase) }).First.ClickAsync();
            await page.WaitForTimeoutAsync(1_000);
            var body = await page.InnerTextAsync("body");
            body.ShouldContain("Map Fields");
            body.ShouldContain("No sources configured");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DetailLineageAndPreviewTabsRenderNavButtons()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit LineageTab_RendersOpenLineageButton / PreviewTab_RendersOpenDataPreviewButton.
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/{name}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(2_000);

            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Lineage", RegexOptions.IgnoreCase) }).First.ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Open Lineage Explorer", RegexOptions.IgnoreCase) }))
                .ToBeVisibleAsync(new() { Timeout = 8_000 });

            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Preview", RegexOptions.IgnoreCase) }).First.ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Open Data Preview", RegexOptions.IgnoreCase) }))
                .ToBeVisibleAsync(new() { Timeout = 8_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DetailAnnotationsTabEmptyThenSeededRows()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit AnnotationsTab_ShowsEmptyMessageWhenNoAnnotations + AnnotationsTab_RendersAnnotationRows.
        // First a freshly-seeded DataSet has the empty annotations state; then we seed one annotation via
        // the API and a SECOND DataSet to read it back (owner/steward/classification/tags render).
        await using var seeder = await ApiSeeder.CreateAsync();
        var empty = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));
        var withAnn = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));
        await seeder.CreateAnnotationAsync(withAnn, owner: "e2eowner", steward: "e2esteward", classification: "Internal", tags: AnnotationTags);

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            // Empty-state DataSet.
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/{empty}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(2_000);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Annotations", RegexOptions.IgnoreCase) }).First.ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("No annotations for this DataSet", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            // The Add Annotation form is always shown.
            await Assertions.Expect(page.GetByPlaceholder("Data owner")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByPlaceholder("Data steward")).ToBeVisibleAsync();

            // Annotated DataSet — the seeded row renders owner/steward/classification (tags are dropped
            // server-side — that bug is asserted separately in AnnotationTagsAreDroppedRED).
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/{withAnn}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(2_000);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Annotations", RegexOptions.IgnoreCase) }).First.ClickAsync();
            await page.WaitForTimeoutAsync(1_500);
            var body = await page.InnerTextAsync("body");
            body.ShouldContain("e2eowner");
            body.ShouldContain("e2esteward");
            body.ShouldContain("Internal");
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// RED — CONFIRMED APP BUG. DataSet annotation TAGS are dropped: seeding an annotation with
    /// <c>tags:["e2etag1"]</c> via <c>POST /catalog/datasets/{name}/annotations</c> (201) and reading it back
    /// via <c>GET …/annotations</c> returns <c>tags:[]</c> (probed live) — owner/steward/classification all
    /// persist, only tags are lost. The UI Annotations tab therefore renders no tag chips for the row. This
    /// asserts the CORRECT behavior (the seeded tag chip renders) and is RED until tag persistence is fixed.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task AnnotationTagsAreDroppedRED()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));
        await seeder.CreateAnnotationAsync(name, owner: "tagowner", steward: null, classification: "Public", tags: UniqueAnnotationTags);

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/{name}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(2_000);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Annotations", RegexOptions.IgnoreCase) }).First.ClickAsync();
            await page.WaitForTimeoutAsync(1_500);
            // CORRECT: the seeded tag renders as a chip. With the bug, tags are lost server-side — RED.
            (await page.InnerTextAsync("body")).ShouldContain("e2euniquetag");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DetailAddAnnotationPersistsAndReadsBack()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit AnnotationsTab_SubmitAnnotation_InvokesOnCreate_WithSplitTags — drive the REAL Add Annotation
        // form (Owner/Steward/Classification/Tags) → Save → the form resets and the new row appears in the
        // list (POST /catalog/datasets/{name}/annotations). The parent DataSet teardown cleans the annotation.
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateDataSetAsync(ApiSeeder.NewPrefix("e2eds"));
        var owner = string.Concat("owner-", Guid.NewGuid().ToString("N").AsSpan(0, 8));

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/{name}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(2_000);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Annotations", RegexOptions.IgnoreCase) }).First.ClickAsync();
            await Assertions.Expect(page.GetByPlaceholder("Data owner")).ToBeVisibleAsync(new() { Timeout = 10_000 });

            await page.GetByPlaceholder("Data owner").FillAsync(owner);
            await page.GetByPlaceholder("Data steward").FillAsync("e2e-steward");
            await page.Locator("select.input").First.SelectOptionAsync(new SelectOptionValue { Value = "Confidential" });
            await page.GetByPlaceholder(new Regex("finance, compliance", RegexOptions.IgnoreCase)).FillAsync("alpha, beta");
            await page.GetByRole(AriaRole.Button, new() { NameString = "Save Annotation" }).ClickAsync();
            await page.WaitForTimeoutAsync(3_000);

            await PageAssertions.ShouldNotShowErrorAsync(page);
            // Read back: the persisted annotation row carries the owner + classification (tags are dropped
            // server-side — covered by AnnotationTagsAreDroppedRED, not asserted here).
            var body = await page.InnerTextAsync("body");
            body.ShouldContain(owner);
            body.ShouldContain("Confidential");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task NotFoundDataSetShowsNotFoundCard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit RendersNotFoundWhenCurrentDataSetNull.
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/e2e-ds-{Guid.NewGuid():N}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2_500);
            await Assertions.Expect(page.GetByText(new Regex("DataSet not found", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- CALCULATED DESIGNER --------------------------------------------

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CalculatedDesignerRendersAndAddsNode()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit CalculatedDesignerPageTests Palette_AddNode_AddsNodeToCanvas. /datasets/calculated/new IS a
        // real @page (CalculatedDesigner.razor) with a working circuit; the operations palette appends nodes
        // (2 rects per node) to the SVG canvas.
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/calculated/new", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = 15_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await Assertions.Expect(page.Locator("h1").Filter(new() { HasTextRegex = new Regex("CALCULATION DESIGNER", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(4_000);

            var rectsBefore = await page.Locator("svg rect").CountAsync();
            var palette = page.Locator(".card").Filter(new() { HasTextString = "OPERATIONS_PALETTE" });
            await palette.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("SOURCE", RegexOptions.IgnoreCase) }).First.ClickAsync();
            await Assertions.Expect(page.Locator("svg rect")).ToHaveCountAsync(rectsBefore + 2, new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P3")]
    public async Task CalculatedDesignerBackNavigatesToDataSets()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // bUnit BackButton_NavigatesToDataSets.
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/calculated/new", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(3_000);
            // The back control is the ghost icon button in the header; click it and assert we land on /datasets.
            await page.Locator("button.btn-ghost").First.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/datasets(?:$|\?)"), new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- WIZARD (RED — confirmed circuit-crash bug) ----------------------

    /// <summary>
    /// RED — CONFIRMED APP BUG. The DataSet create/edit wizard (/datasets/new) cannot be used: its Blazor
    /// InteractiveServer circuit throws during the initial render batch and is terminated on connect
    /// ("Error: There was an error applying batch 2." → "...this circuit will be terminated."). After the
    /// circuit dies every @onclick is dead — NEXT_SEQUENCE never advances the step and COMMENCE_BUILD is
    /// unreachable, so a DataSet can never be created or edited through the UI (the prime suspect is the
    /// OptionPicker&lt;IDataSetCategory&gt; on step 0). This test asserts the CORRECT behavior (the wizard
    /// advances past step 0) and is RED until the crash is fixed in FDW.
    ///
    /// bUnit-only as a consequence: every DataSetWizardPageTests per-step branch (Step0 service-option
    /// fallback, Step1 add-field / import panel / field-type fallback, Step2 add-source / protocol fallback,
    /// Step3 mapping sync, Step4 join-linkage, Step5 finalize+submit, Previous/Terminate) is physically
    /// unreachable end-to-end because the circuit is dead before any of it can be driven. Those branches are
    /// covered by the bUnit suite and listed here as bUnit-only; they CANNOT be made green in E2E until this
    /// bug is fixed. (DataSet fixtures are seeded via the API instead — see the list/detail tests above.)
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task WizardCircuitCrashesOnConnect()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/datasets/new", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var nameBox = page.GetByPlaceholder("DATA_SET_IDENTIFIER");
            await nameBox.WaitForAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(3_000);
            await nameBox.FillAsync(string.Concat("e2e-ds-", Guid.NewGuid().ToString("N").AsSpan(0, 8)));

            // CORRECT behavior: clicking NEXT_SEQUENCE advances to step 2 of 6, surfacing a PREVIOUS button.
            // With the circuit dead this never happens — assertion fails RED, documenting the bug.
            await page.GetByRole(AriaRole.Button, new() { Name = "NEXT_SEQUENCE", Exact = true }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "PREVIOUS" }))
                .ToBeVisibleAsync(new() { Timeout = 8_000 });
        }
        finally { await ctx.CloseAsync(); }
    }
}
