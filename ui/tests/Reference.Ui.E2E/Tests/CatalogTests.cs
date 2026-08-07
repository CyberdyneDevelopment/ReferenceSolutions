using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Data Catalog (/catalog). This page is an intentional placeholder: Catalog.razor renders a static card
/// and a comment "AWAITING: CatalogProvider component" — there is no data load, no interactivity, and no
/// branches (a server-side <c>GET /catalog/search</c> exists but no UI consumes it). Coverage is therefore
/// a single render assertion: the placeholder copy paints with no error boundary. This is GREEN and is the
/// regression net that holds the page honest until a real CatalogProvider is wired in.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class CatalogTests(PlaywrightFixture fx)
{
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CatalogRendersPlaceholder()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/catalog", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldBeAuthenticatedAsync(page);
            await PageAssertions.ShouldNotShowErrorAsync(page);
            // Title + the placeholder description + the AWAITING note.
            await Assertions.Expect(page.Locator("h1").Filter(new() { HasTextRegex = new Regex("Data Catalog", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(new Regex("searchable index of all DataSets", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(new Regex("AWAITING: CatalogProvider component", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }
}
