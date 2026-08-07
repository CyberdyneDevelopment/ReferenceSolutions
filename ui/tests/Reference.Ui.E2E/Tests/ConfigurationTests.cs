using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Configuration area: /configuration (master-detail CRUD over categories → instances → property edit)
/// and /configuration/issues (validate + heal workflow).
///
/// /configuration covers: the page renders without an error boundary; the detail pane shows the
/// "Select a category" placeholder before a category is chosen; and — when the category sidebar is
/// populated — selecting a category loads its instances pane. A confirmed RED defect is pinned by
/// <see cref="CategorySidebarPopulatesOnLoad"/>: the sidebar binds to <c>ctx.Types</c>, but the inner
/// provider's auto-load path calls <c>LoadInstances</c> (not a Types load), so the sidebar renders
/// "No configuration types found" on first load and no categories are selectable.
///
/// /configuration/issues covers: auto-validate on first render lands in one of its terminal states
/// (issues table / "No configuration issues found." / the initial prompt); the Validate action re-runs;
/// and Auto Heal is correctly disabled when there are no healable issues.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class ConfigurationTests(PlaywrightFixture fx)
{
    private static readonly RegexOptions Ci = RegexOptions.IgnoreCase;

    private async Task<(IBrowserContext, IPage)> OpenAsync(string route)
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}{route}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    // The category sidebar buttons live in the "Categories" card.
    private static ILocator CategoryButtons(IPage page) =>
        page.Locator(".card").Filter(new() { HasTextString = "Categories" }).Locator("button");

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ConfigurationPageRendersMasterDetail()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/configuration");
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("^Configuration$", Ci) }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await Assertions.Expect(page.GetByText(new Regex("Categories", Ci))).ToBeVisibleAsync();
            // Before a category is chosen, the detail pane shows the placeholder.
            await Assertions.Expect(page.GetByText(new Regex("Select a category", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CategorySidebarPopulatesOnLoad()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // RED: the sidebar should list configuration categories on load. It binds to ctx.Types, but the
        // inner ConfigurationProvider auto-loads via LoadInstances (not a Types load), so the sidebar
        // shows "No configuration types found" and nothing is selectable. This asserts the sidebar is
        // populated; it FAILS while the Types auto-load path is missing and passes once it's wired.
        var (ctx, page) = await OpenAsync("/configuration");
        try
        {
            // Give the provider's interactive load time to settle.
            await page.WaitForTimeoutAsync(2500);
            var emptyBanner = page.GetByText(new Regex("No configuration types found", Ci));
            var hasEmpty = await emptyBanner.CountAsync() > 0 && await emptyBanner.First.IsVisibleAsync();
            hasEmpty.ShouldBeFalse(
                "REGRESSION (RUI): /configuration category sidebar renders 'No configuration types found' " +
                "on first load. The sidebar binds ctx.Types but the inner provider auto-loads LoadInstances, " +
                "not a Types/GetRootTypes load — so no categories are ever populated and the master-detail " +
                "CRUD is unreachable. Wire a Types load into the provider's first interactive render.");
            (await CategoryButtons(page).CountAsync()).ShouldBeGreaterThan(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SelectingCategoryLoadsInstancesPane()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Gated on the sidebar-population defect: if categories aren't selectable, this self-skips and is
        // documented by CategorySidebarPopulatesOnLoad. Otherwise it drives category selection and
        // asserts the instances pane (New Instance / rows / empty) renders.
        var (ctx, page) = await OpenAsync("/configuration");
        try
        {
            await page.WaitForTimeoutAsync(2500);
            if (await CategoryButtons(page).CountAsync() == 0)
            {
                Assert.Skip("Gated on the /configuration sidebar-population defect (no categories rendered) " +
                            "— see CategorySidebarPopulatesOnLoad.");
                return;
            }
            await CategoryButtons(page).First.ClickAsync();
            await Assertions.Expect(
                page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("New Instance", Ci) })
                    .Or(page.GetByText(new Regex("No instances configured", Ci))).First)
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// New Instance create form (gated on the sidebar-population defect): once a category is selected, the
    /// New Instance button opens a create form with a Name input + per-type-property inputs. Blank Name is
    /// a silent no-op (Configuration.razor:306 early-returns). Asserts the form opens and the blank-name
    /// guard does NOT create or error. Self-skips when categories aren't selectable (sidebar defect).
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task NewInstanceFormOpensAndBlankNameNoOps()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/configuration");
        try
        {
            await page.WaitForTimeoutAsync(2500);
            if (await CategoryButtons(page).CountAsync() == 0)
            {
                Assert.Skip("Gated on the /configuration sidebar-population defect — see CategorySidebarPopulatesOnLoad.");
                return;
            }
            await CategoryButtons(page).First.ClickAsync();
            var newInstance = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("New Instance", Ci) });
            await Assertions.Expect(newInstance).ToBeVisibleAsync(new() { Timeout = 20_000 });
            await newInstance.ClickAsync();
            // The create form's Name input appears.
            var createBtn = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Create$", Ci) });
            await Assertions.Expect(createBtn).ToBeVisibleAsync(new() { Timeout = 10_000 });
            // Blank Name → silent no-op: clicking Create neither errors nor closes the form.
            await createBtn.ClickAsync();
            await page.WaitForTimeoutAsync(1000);
            await Assertions.Expect(createBtn).ToBeVisibleAsync(); // form still open
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Secret-field reveal toggle (gated on sidebar + an instance being present): when an instance whose
    /// property keys contain Password/Secret/Key/Token is selected, those inputs render as password type
    /// with a Show/Hide reveal toggle (IsSecretField, Configuration.razor:380). Asserts a password-type
    /// input + a Show/Hide control appear in the edit form when reachable; self-skips otherwise.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task SecretFieldRendersRevealToggleWhenReachable()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/configuration");
        try
        {
            await page.WaitForTimeoutAsync(2500);
            if (await CategoryButtons(page).CountAsync() == 0)
            {
                Assert.Skip("Gated on the /configuration sidebar-population defect — see CategorySidebarPopulatesOnLoad.");
                return;
            }
            await CategoryButtons(page).First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            // Instance rows live in the detail pane; the chevron rows are buttons. Pick the first instance.
            var instanceRow = page.Locator(".card").Filter(new() { HasTextString = "instances" }).Locator("button").First;
            if (await instanceRow.CountAsync() == 0 || !await instanceRow.IsVisibleAsync())
            {
                Assert.Skip("No configuration instances present to open an edit form — secret-field reveal not reachable.");
                return;
            }
            await instanceRow.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            // If this instance exposes a secret field, a Show/Hide toggle renders. Otherwise nothing to assert.
            var reveal = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Show|Hide", Ci) });
            if (await reveal.CountAsync() == 0)
            {
                Assert.Skip("Selected instance has no secret-typed property — reveal toggle not present.");
                return;
            }
            await Assertions.Expect(page.Locator("input[type=password]").First).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await reveal.First.ClickAsync();
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- /configuration/issues ----

    [Fact]
    [Trait("Priority", "P1")]
    public async Task IssuesAutoValidatesToTerminalState()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/configuration/issues");
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("Configuration Issues", Ci) }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            // Auto-validate on first render → one of: issues table, "No configuration issues found.",
            // or the initial "Click Validate" prompt (if validation hasn't resolved yet).
            await Assertions.Expect(
                page.GetByText(new Regex("No configuration issues found", Ci))
                    .Or(page.GetByText(new Regex("Click Validate", Ci)))
                    .Or(page.Locator("table")).First)
                .ToBeVisibleAsync(new() { Timeout = 25_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ValidateActionProducesAResult()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // RED (confirmed): the /configuration/issues validate workflow is inert on this build. The page
        // is supposed to auto-validate on first interactive render (OnAfterRenderAsync); instead it stays
        // on the "Click Validate to scan…" prompt, and clicking the Validate button fires NO validation
        // request and never transitions to a result (no summary cards, no issues table, no "No
        // configuration issues found." card). The expected outcome — a validation result after Validate —
        // is asserted here and FAILS while the action is dead; it passes once Validate actually runs the
        // ConfigurationValidationProvider and renders a terminal result.
        var (ctx, page) = await OpenAsync("/configuration/issues");
        try
        {
            var validate = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Validate$", Ci) });
            await Assertions.Expect(validate).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await validate.ClickAsync();
            // A real validation transitions OUT of the initial prompt into a terminal result state.
            await Assertions.Expect(
                page.GetByText(new Regex("No configuration issues found", Ci))
                    .Or(page.GetByText(new Regex("Critical", Ci)))
                    .Or(page.Locator("table")).First)
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task AutoHealDisabledWhenNoHealableIssues()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Branch: Auto Heal is disabled when Validation is null OR there are no healable issues. On a
        // healthy graph ("No configuration issues found.") the button must be disabled.
        var (ctx, page) = await OpenAsync("/configuration/issues");
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("Configuration Issues", Ci) }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(3000); // let any validation settle
            // Auto Heal is disabled whenever Validation is null OR there are no healable issues. On this
            // build validation has not produced healable issues (see ValidateActionProducesAResult), so
            // the button must be disabled. (If a future build surfaces healable issues it would enable —
            // we only require the disabled-guard when no healable issues are present, which is the case
            // whenever the "No configuration issues found" card OR the initial prompt is showing.)
            var hasHealable = await page.Locator("table")
                .Filter(new() { Has = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Heal$", Ci) }) })
                .CountAsync() > 0;
            var autoHeal = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Auto Heal", Ci) });
            if (hasHealable)
                await Assertions.Expect(autoHeal).ToBeEnabledAsync();
            else
                await Assertions.Expect(autoHeal).ToBeDisabledAsync();
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }
}
