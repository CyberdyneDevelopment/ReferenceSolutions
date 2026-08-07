using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Settings area: /settings (sidebar with General / Appearance / Notifications / Security sections),
/// /settings/appearance (theme CRUD), and /settings/notifications (per-type channel toggles).
///
/// Covers: each section card renders; General Save shows the "Settings saved." success branch and a
/// persist round-trip read-back; Security Save; the Appearance theme grid + New Theme modal +
/// blank-name no-op; and the Notifications-settings page render. Two confirmed RED defects are pinned
/// by tests:
///   • <see cref="GeneralSettingsPersistAcrossReload"/> — General fields are hardcoded local state
///     ("Fdw"/"UTC"/"YYYY-MM-DD") and are NOT seeded from persisted values on load
///     (Settings.razor — comment at :191 claims init-from-provider but no such code exists), so a
///     Save→reload round-trip does NOT survive.
///   • <see cref="NotificationsTabSaveIsWiredUp"/> — the Notifications-section "Save Changes" button has
///     no @onclick (Settings.razor:145); clicking persists nothing.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class SettingsTests(PlaywrightFixture fx)
{
    private static readonly RegexOptions Ci = RegexOptions.IgnoreCase;

    private async Task<(IBrowserContext, IPage)> OpenAsync(string route = "/settings")
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}{route}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    private static ILocator Section(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex($"^{name}$", Ci) });

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EachSectionRendersItsCard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("^Settings$", Ci) }))
                .ToBeVisibleAsync();
            // General (default) shows the System Name input.
            await Assertions.Expect(page.GetByText(new Regex("System Name", Ci))).ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Section(page, "Appearance").ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("Manage Themes", Ci))).ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Section(page, "Notifications").ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("Pipeline Failures", Ci))).ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Section(page, "Security").ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("Session Timeout", Ci))).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task GeneralSaveShowsSuccess()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // General Save calls OnUpdateSetting 3× then, if no ErrorMessage, sets "Settings saved."
        var (ctx, page) = await OpenAsync();
        try
        {
            var sysName = page.Locator("input.input").First;
            await Assertions.Expect(sysName).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await sysName.FillAsync($"e2e-ops-{Guid.NewGuid():N}"[..18]);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save Changes", Ci) }).First.ClickAsync();
            // Success branch surfaces the green "Settings saved." message.
            await Assertions.Expect(page.GetByText(new Regex("Settings saved", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task GeneralSettingsPersistAcrossReload()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // RED (suspect persistence): change System Name → Save (success) → reload the page → read back.
        // The field is initialised from hardcoded local state, NOT from ctx.GetSettingValue, so after a
        // reload it reverts to the literal "Fdw" default and the saved value is lost. This
        // assertion FAILS while that defect exists and starts passing once the form seeds from persisted
        // values on load.
        var unique = $"e2e-ops-{Guid.NewGuid():N}"[..18];
        var (ctx, page) = await OpenAsync();
        try
        {
            var sysName = page.Locator("input.input").First;
            await Assertions.Expect(sysName).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await sysName.FillAsync(unique);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save Changes", Ci) }).First.ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("Settings saved", Ci))).ToBeVisibleAsync(new() { Timeout = 20_000 });

            // Reload and read back the persisted value.
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/settings", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var reloaded = page.Locator("input.input").First;
            await Assertions.Expect(reloaded).ToBeVisibleAsync(new() { Timeout = 10_000 });
            (await reloaded.InputValueAsync()).ShouldBe(unique,
                "REGRESSION (RUI): General settings do not round-trip. The System Name field is hardcoded " +
                "local state and is not seeded from persisted values on load (Settings.razor ~:191), so a " +
                "Save→reload loses the saved value. Seed the form from ctx.GetSettingValue on load.");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SecuritySaveShowsSuccess()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // RED (confirmed backend defect): Security Save calls OnUpdateSetting for "SessionTimeoutMinutes"
        // and "Enable2FA". The "Enable2FA" update FAILS at the API — the provider surfaces
        // "SettingsProvider: Failed to update setting 'Enable2FA'" (UpdateSettingFailed,
        // SettingsProvider.razor:163; backend UpdateServerSettingEndpoint rejects the Enable2FA key), so
        // the success branch ("Settings saved.") never renders. This asserts the success message; it
        // FAILS while the Enable2FA setting cannot be persisted and passes once the backend accepts it.
        var (ctx, page) = await OpenAsync();
        try
        {
            await Section(page, "Security").ClickAsync();
            var timeout = page.Locator("input[type=number]").First;
            await Assertions.Expect(timeout).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await timeout.FillAsync("45");
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save Changes", Ci) }).First.ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("Settings saved", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task NotificationsTabSaveIsWiredUp()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Fixed: the Notifications-section Save Changes button is wired to SaveNotificationSettings, which
        // persists each toggle via OnUpdateSetting and renders the confirmation. (The toggles are <button>
        // controls, not checkboxes — toggling isn't required to exercise the Save handler.)
        var (ctx, page) = await OpenAsync();
        try
        {
            await Section(page, "Notifications").ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save Changes", Ci) }).First.ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("settings saved", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- /settings/appearance (theme CRUD) ----

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AppearanceRendersThemeGrid()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/settings/appearance");
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("^Themes$", Ci) }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            // Either themes render OR the empty card — both are healthy terminal states.
            await Assertions.Expect(
                page.Locator("h3").First
                    .Or(page.GetByText(new Regex("No themes configured", Ci))).First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task NewThemeModalOpensAndBlankNameNoOps()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/settings/appearance");
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("New Theme", Ci) }).ClickAsync();
            var nameInput = page.GetByPlaceholder(new Regex("My Theme", Ci));
            await Assertions.Expect(nameInput).ToBeVisibleAsync(new() { Timeout = 10_000 });
            // Branch: blank Name → SaveTheme early-returns (no save, modal stays open).
            await nameInput.FillAsync("");
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Save$", Ci) }).ClickAsync();
            await page.WaitForTimeoutAsync(700);
            await Assertions.Expect(nameInput).ToBeVisibleAsync(); // still open
            await PageAssertions.ShouldNotShowErrorAsync(page);
            // Cancel closes it.
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Cancel$", Ci) }).ClickAsync();
            await Assertions.Expect(nameInput).ToBeHiddenAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateThemeRoundTripsAndSelfCleans()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Test-owned fixture: create a uniquely-named theme, assert it appears exactly once, then delete.
        var name = $"e2e-ops-{Guid.NewGuid():N}"[..16] + "-thm";
        var (ctx, page) = await OpenAsync("/settings/appearance");
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("New Theme", Ci) }).ClickAsync();
            await page.GetByPlaceholder(new Regex("My Theme", Ci)).FillAsync(name);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Save$", Ci) }).ClickAsync();

            var card = page.Locator("h3").Filter(new() { HasTextString = name });
            await Assertions.Expect(card.First).ToBeVisibleAsync(new() { Timeout = 20_000 });
            (await card.CountAsync()).ShouldBe(1);
        }
        finally
        {
            try
            {
                var themeCard = page.Locator(".grid > div, .card").Filter(new() { HasTextString = name }).First;
                await themeCard.HoverAsync();
                var del = themeCard.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("delete|remove", Ci) });
                if (await del.CountAsync() > 0) { await del.First.ClickAsync(); await page.WaitForTimeoutAsync(1200); }
            }
            catch { /* best-effort cleanup */ }
            await ctx.CloseAsync();
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SetDefaultThemeMovesDefaultBadge()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Test-owned fixture: create a uniquely-named theme, click its "Set Default", and assert the
        // theme's card now carries the "Default" badge (provider reloads themes → the marker moves). The
        // newly-default theme cannot self-delete (delete is hidden for the default), so cleanup re-defaults
        // a different theme first, then deletes ours.
        var name = $"e2e-ops-{Guid.NewGuid():N}"[..16] + "-dft";
        var (ctx, page) = await OpenAsync("/settings/appearance");
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("New Theme", Ci) }).ClickAsync();
            await page.GetByPlaceholder(new Regex("My Theme", Ci)).FillAsync(name);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Save$", Ci) }).ClickAsync();

            var card = page.Locator(".card").Filter(new() { HasTextString = name }).First;
            await Assertions.Expect(card).ToBeVisibleAsync(new() { Timeout = 20_000 });
            await card.HoverAsync();
            await card.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Set Default", Ci) }).ClickAsync();
            // After set-default the provider reloads; our card must now show the Default badge.
            await Assertions.Expect(card.GetByText(new Regex("^Default$", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        finally
        {
            try
            {
                // Re-default a different theme so ours becomes deletable, then delete ours.
                var other = page.Locator(".card").Filter(new() { Has = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Set Default", Ci) }) }).First;
                if (await other.CountAsync() > 0)
                {
                    await other.HoverAsync();
                    await other.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Set Default", Ci) }).First.ClickAsync();
                    await page.WaitForTimeoutAsync(1500);
                }
                var mine = page.Locator(".card").Filter(new() { HasTextString = name }).First;
                await mine.HoverAsync();
                var del = mine.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("delete|remove", Ci) });
                if (await del.CountAsync() > 0) { await del.First.ClickAsync(); await page.WaitForTimeoutAsync(1200); }
            }
            catch { /* best-effort cleanup */ }
            await ctx.CloseAsync();
        }
    }

    // ---- /settings/notifications (per-type channel toggles) ----

    [Fact]
    [Trait("Priority", "P1")]
    public async Task NotificationSettingsPageRenders()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/settings/notifications");
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("Notification Preferences", Ci) }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            // Either the prefs list renders (Save/Reset present) or — when the NameIdentifier claim is
            // present — toggles appear. Healthy render is the assertion.
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await Assertions.Expect(
                page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Save$", Ci) })
                    .Or(page.Locator("input[type=checkbox]")).First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// RED (confirmed bug) — bUnit branches <c>ToggleCheckboxFlipsState</c> + <c>SaveInvokesApiWithoutError</c>
    /// end-to-end. /settings/notifications IS wired (unlike the inert Notifications TAB on /settings): flip
    /// the first preference checkbox, Save (the page re-maps from the response), then RELOAD and assert the
    /// flipped state survives. VERIFIED FAILING against ui-ctc: after Save + reload the toggle reverts to
    /// its prior value — the notification preference does not persist across a reload. This is the persist
    /// round-trip the inert settings-tab Save cannot make; it pins a real persistence bug. Self-restoring.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task NotificationPreferenceTogglePersistsAcrossReload()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/settings/notifications");
        try
        {
            var first = page.Locator("input[type=checkbox]").First;
            // The page only renders the prefs list when the NameIdentifier claim resolves; if no toggles
            // render on this build, there is nothing to persist-test — skip rather than false-fail.
            if (await first.CountAsync() == 0 || !await first.IsVisibleAsync())
            {
                Assert.Skip("No notification-preference toggles rendered (NameIdentifier claim path) — render covered by NotificationSettingsPageRenders.");
                return;
            }
            var before = await first.IsCheckedAsync();
            await first.SetCheckedAsync(!before);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Save$", Ci) }).ClickAsync();
            await page.WaitForTimeoutAsync(2000);
            await PageAssertions.ShouldNotShowErrorAsync(page);

            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/settings/notifications", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var reloaded = page.Locator("input[type=checkbox]").First;
            await Assertions.Expect(reloaded).ToBeVisibleAsync(new() { Timeout = 15_000 });
            (await reloaded.IsCheckedAsync()).ShouldBe(!before,
                "Notification preference toggle did not persist across reload (Save→re-map path).");

            // Restore original state.
            await reloaded.SetCheckedAsync(before);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Save$", Ci) }).ClickAsync();
            await page.WaitForTimeoutAsync(1000);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// bUnit branch <c>ResetReloadsPreferences</c>: flip a toggle WITHOUT saving, click Reset, and the
    /// toggle reverts to its persisted value (Reset → LoadPreferences re-reads from the server). Asserts
    /// the discard-changes behavior end-to-end.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task NotificationResetDiscardsUnsavedToggle()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync("/settings/notifications");
        try
        {
            var first = page.Locator("input[type=checkbox]").First;
            var reset = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Reset$", Ci) });
            if (await first.CountAsync() == 0 || !await first.IsVisibleAsync() || await reset.CountAsync() == 0)
            {
                Assert.Skip("No notification-preference toggles/Reset rendered on this build.");
                return;
            }
            var persisted = await first.IsCheckedAsync();
            await first.SetCheckedAsync(!persisted); // unsaved change
            await reset.First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            // Reset re-loads from the server → the toggle reverts to its persisted value.
            await Assertions.Expect(page.Locator("input[type=checkbox]").First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            (await page.Locator("input[type=checkbox]").First.IsCheckedAsync()).ShouldBe(persisted,
                "Reset did not discard the unsaved toggle back to the persisted value.");
        }
        finally { await ctx.CloseAsync(); }
    }
}
