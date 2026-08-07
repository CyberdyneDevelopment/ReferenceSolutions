using Xunit;
using Microsoft.Playwright;

namespace Reference.Ui.E2E.Infrastructure;

/// <summary>
/// Shared Playwright browser + a one-time authenticated session. Logs in through the REAL UI
/// login form once, captures the cookie via <c>storageState</c>, and hands every test a fresh
/// authenticated <see cref="IPage"/> (real browser context) — so tests exercise the rendered UI,
/// not the API.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _storageStatePath;

    public static string BaseUrl => E2ESettings.BaseUrl
        ?? throw new InvalidOperationException("E2E_BASE_URL is not set.");

    public async ValueTask InitializeAsync()
    {
        if (!E2ESettings.Enabled)
            return; // tests self-skip; don't spin up a browser for nothing

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !E2ESettings.Headed,
        });

        // One-time login through the actual UI so the saved cookie matches a real session.
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Why: the SSR login form uses stable element ids (#username/#password) — the visible labels
        // are themed copy ("Identity"/"Passcode"), so a label-text locator is brittle. Bind to the ids.
        await page.Locator("#username").FillAsync(E2ESettings.Username);
        await page.Locator("#password").FillAsync(E2ESettings.Password);
        await page.Locator("button[type=submit]").ClickAsync();

        // Landed off /login = authenticated.
        await page.WaitForURLAsync(u => !u.Contains("/login", StringComparison.Ordinal),
            new PageWaitForURLOptions { Timeout = 30_000 });

        _storageStatePath = Path.Combine(Path.GetTempPath(), $"ref-ui-e2e-{Guid.NewGuid():N}.json");
        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = _storageStatePath });
        await context.CloseAsync();
    }

    /// <summary>A fresh authenticated browser page. Caller disposes the returned context.</summary>
    public async Task<(IBrowserContext Context, IPage Page)> NewAuthenticatedPageAsync()
    {
        var ctx = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            StorageStatePath = _storageStatePath,
        });
        return (ctx, await ctx.NewPageAsync());
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_storageStatePath is not null && File.Exists(_storageStatePath)) File.Delete(_storageStatePath);
    }
}

/// <summary>xUnit collection so the browser + login happen once for the whole suite.</summary>
[CollectionDefinition("ui-e2e")]
public sealed class UiE2ECollectionDefinition : ICollectionFixture<PlaywrightFixture> { }
