using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Profile area (/profile): user-info card + Change Password form + Set Preference form.
///
/// Covers: render of the user-info card; the three client-side change-password validation branches
/// (blank current / new &lt;8 chars / mismatch — Profile.razor:161/166/170); the blank-preference-key
/// silent no-op (Profile.razor:190); and a Set Preference round-trip with a unique guid-prefixed key
/// (provider reloads the profile so the new pref appears). Password changes are NOT submitted with a
/// valid payload — that would mutate the shared admin login the whole suite authenticates with; only
/// the client-side guard branches are exercised.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class ProfileTests(PlaywrightFixture fx)
{
    private static readonly RegexOptions Ci = RegexOptions.IgnoreCase;

    private async Task<(IBrowserContext, IPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/profile", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        // Provider loads on first interactive render; wait for the user-info card to paint.
        await Assertions.Expect(page.GetByText(new Regex("User Information", Ci)))
            .ToBeVisibleAsync(new() { Timeout = 20_000 });
        return (ctx, page);
    }

    private static ILocator PwField(IPage page, int n) => page.Locator("input[type=password]").Nth(n);

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RendersUserInformationCard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("^Profile$", Ci) }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(new Regex("Change Password", Ci)).First).ToBeVisibleAsync();
            // The logged-in username (admin) appears in the card.
            (await page.InnerTextAsync("body")).ShouldContain(E2ESettings.Username);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ChangePasswordBlankCurrentShowsRequired()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Change Password", Ci) }).ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("Current password is required", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ChangePasswordShortNewShowsMinLength()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await PwField(page, 0).FillAsync("whatever-current");
            await PwField(page, 1).FillAsync("short");   // < 8 chars
            await PwField(page, 2).FillAsync("short");
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Change Password", Ci) }).ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("at least 8 characters", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ChangePasswordMismatchShowsError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await PwField(page, 0).FillAsync("whatever-current");
            await PwField(page, 1).FillAsync("longenough1");
            await PwField(page, 2).FillAsync("longenough2");   // mismatch
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Change Password", Ci) }).ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("New passwords do not match", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task BlankPreferenceKeyIsSilentNoOp()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            // Set Preference with a blank key → SubmitPreference early-returns (no-op, no error).
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Set$", Ci) }).ClickAsync();
            await page.WaitForTimeoutAsync(700);
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task SetPreferenceRoundTrips()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Test-owned data: write a uniquely-keyed preference, assert it appears after the provider
        // reloads the profile. Key is guid-prefixed so it never collides with ambient prefs.
        var key = $"e2e-ops-{Guid.NewGuid():N}"[..16];
        var value = "on";
        var (ctx, page) = await OpenAsync();
        try
        {
            // The two preference inputs are the only plain text inputs in the Preferences card; bind by
            // order within that card region. Use the last two text inputs on the page (key, value).
            var textInputs = page.Locator("input:not([type=password]):not([type=number]):not([type=checkbox]):not([type=color])");
            var count = await textInputs.CountAsync();
            await textInputs.Nth(count - 2).FillAsync(key);
            await textInputs.Nth(count - 1).FillAsync(value);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Set$", Ci) }).ClickAsync();

            // On success the provider reloads the profile and the new key/value renders in the prefs list.
            await Assertions.Expect(page.GetByText(new Regex(Regex.Escape(key), Ci)))
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }
}
