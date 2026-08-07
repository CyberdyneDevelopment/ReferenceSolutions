using Microsoft.Playwright;

namespace Reference.Ui.E2E.Infrastructure;

/// <summary>Shared UI-level assertions — all operate on the rendered DOM, never on API responses.</summary>
public static class PageAssertions
{
    /// <summary>The Blazor/error-boundary failure markers that must never be visible on a healthy page.</summary>
    private static readonly string[] FailureTexts =
    {
        "An unhandled error has occurred",
        "An error has occurred",
        "provider not found",
        "BaseAddress must be set",
        "There was an unhandled exception",
    };

    /// <summary>Fails if the rendered page is showing any error boundary / known failure banner.</summary>
    public static async Task ShouldNotShowErrorAsync(IPage page)
    {
        var body = (await page.InnerTextAsync("body")) ?? string.Empty;
        foreach (var marker in FailureTexts)
            if (body.Contains(marker, StringComparison.OrdinalIgnoreCase))
                throw new Xunit.Sdk.XunitException(
                    $"Page {page.Url} rendered an error: '{marker}'.");
    }

    /// <summary>Waits for the page to settle and asserts it didn't bounce to /login (auth held).</summary>
    public static async Task ShouldBeAuthenticatedAsync(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (page.Url.Contains("/login", StringComparison.Ordinal))
            throw new Xunit.Sdk.XunitException($"Redirected to /login — session not authenticated ({page.Url}).");
    }
}
