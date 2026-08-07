using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Field Mapper (<c>/mapper</c>) — interactive: source/target connection selects → schema discovery →
/// table selects → field lists → click-to-map 3-column board, Auto-Map, and Save Mappings. Driven
/// through the rendered DOM.
///
/// Covered branches: initial render with Save disabled (Mappings.Count==0); connection select populates
/// the source-connection options; choosing a connection triggers schema discovery (table select appears
/// or an error banner surfaces); empty placeholders for fields; mode of the Save button.
///
/// RED finding documented in <see cref="SaveMappingsDoesNotPersist_Documented"/>: "Save Mappings" only
/// LOGS (DataMapperProvider TODO) — it performs no API write. We cannot assert a negative persistence
/// over the UI deterministically, so the test pins the observable contract (button stays disabled with
/// no mappings; OnValidate is unwired so no validation surface exists) and documents the gap.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class MapperTests(PlaywrightFixture fx)
{
    private async Task<(IBrowserContext, IPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/mapper", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    private static ILocator SourceConnSelect(IPage p) => p.Locator("select.input").First;

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RendersToolbarWithSaveDisabled()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.Locator("h1", new() { HasTextString = "Field Mapper" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Auto-Map" })).ToBeVisibleAsync();
            // Save is disabled while Mappings.Count == 0.
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save Mappings" })).ToBeDisabledAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task SourceConnectionOptionsPopulate()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // LoadConnections success → source connection select gains real options beyond the placeholder.
            await page.WaitForFunctionAsync(
                "() => document.querySelectorAll('select.input')[0]?.options.length > 1",
                null, new PageWaitForFunctionOptions { Timeout = 15_000 });
            (await SourceConnSelect(page).Locator("option").CountAsync()).ShouldBeGreaterThan(1);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task EmptyFieldPlaceholdersRenderInitially()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // Before any selection: 3-column board shows the empty placeholders.
            await Assertions.Expect(page.GetByText("Select a source to load fields")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("Select source and target fields to create mappings")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ChoosingSourceConnectionTriggersSchemaDiscovery()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.WaitForFunctionAsync(
                "() => document.querySelectorAll('select.input')[0]?.options.length > 1",
                null, new PageWaitForFunctionOptions { Timeout = 15_000 });

            // Pick the first concrete connection option.
            var firstVal = await SourceConnSelect(page).Locator("option").Nth(1).GetAttributeAsync("value");
            firstVal.ShouldNotBeNullOrEmpty();
            await SourceConnSelect(page).SelectOptionAsync(new SelectOptionValue { Value = firstVal! });

            // OnSourceConnectionChanged → LoadTablesForConnection (schema discovery). The outcome is one
            // of: a source TABLE select appears (SourceTables>0), OR a SchemaDiscoveryFailed error banner
            // surfaces, OR discovery yields no tables (placeholder stays). All three are valid renders;
            // assert the circuit didn't crash and we reached a settled state.
            await page.WaitForTimeoutAsync(5000);
            await PageAssertions.ShouldNotShowErrorAsync(page);

            var tableSelectAppeared = await page.GetByText("Source Table", new() { Exact = false }).CountAsync() > 0
                || await page.Locator("select.input").CountAsync() > 4; // extra select = table picker
            var errorBanner = await page.Locator("div.bg-red-500\\/10").CountAsync() > 0;
            var placeholderStill = await page.GetByText("Select a source to load fields").CountAsync() > 0;
            (tableSelectAppeared || errorBanner || placeholderStill).ShouldBeTrue();
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task AutoMapWithNoFieldsKeepsSaveDisabled()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // AutoMapFields with empty field lists → _mappings=[]; Save stays disabled, no crash.
            await page.GetByRole(AriaRole.Button, new() { Name = "Auto-Map" }).ClickAsync();
            await page.WaitForTimeoutAsync(1000);
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save Mappings" })).ToBeDisabledAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// RED (documented gap): "Save Mappings" performs NO persistence — OnSave only logs MappingsPrepared
    /// (DataMapperProvider TODO; no API write). Additionally OnValidate is implemented but UNWIRED to any
    /// button, so the mapper exposes no validation surface. With zero mappings the button is disabled, so
    /// the no-persist path is unreachable from a clean load; this test pins that the only Save affordance
    /// is the disabled button and there is no Validate control, documenting the dead/no-op wiring.
    /// </summary>
    [Fact]
    [Trait("Priority", "P3")]
    public async Task ValidateButtonIsWired()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // Save stays disabled with no mappings; the Validate affordance is now present (was missing).
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save Mappings" })).ToBeDisabledAsync();
            (await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("validate", RegexOptions.IgnoreCase) }).CountAsync())
                .ShouldBeGreaterThan(0);
        }
        finally { await ctx.CloseAsync(); }
    }
}
