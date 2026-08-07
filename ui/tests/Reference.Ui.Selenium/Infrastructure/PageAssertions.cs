using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Reference.Ui.Selenium.Infrastructure;

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

    /// <summary>
    /// Waits for the document to finish loading AND for the Blazor circuit to paint real content.
    /// Never a fixed sleep — WebDriverWait polls until the condition holds.
    /// </summary>
    public static void WaitForBlazor(IWebDriver driver, int timeoutSeconds = 30)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        wait.Until(d => string.Equals(
            ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState") as string,
            "complete", StringComparison.Ordinal));
        // Why: readyState=complete fires before the Blazor Server circuit renders interactive
        // content — poll until the body carries visible text so assertions see the real page.
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript(
            "return document.body !== null && document.body.innerText.trim().length > 0") is true);
        // Why: error boundaries and async data can land AFTER first paint (Playwright's NetworkIdle
        // catches this; readyState doesn't) — poll until the rendered content length is stable
        // across two consecutive samples so late failures are visible to the assertions.
        var previous = -1L;
        wait.Until(d =>
        {
            if (((IJavaScriptExecutor)d).ExecuteScript("return document.body.innerText.length") is not long current)
                return false; // body vanished mid-poll — not stable, keep polling (timeout throws)
            var stable = current == previous;
            previous = current;
            return stable;
        });
    }

    /// <summary>Fails if the rendered page is showing any error boundary / known failure banner.</summary>
    public static void ShouldNotShowError(IWebDriver driver)
    {
        var body = driver.FindElement(By.TagName("body")).Text;
        foreach (var marker in FailureTexts)
            if (body.Contains(marker, StringComparison.OrdinalIgnoreCase))
                throw new Xunit.Sdk.XunitException(
                    $"Page {driver.Url} rendered an error: '{marker}'.");
    }

    /// <summary>Fails if the page produced a blank shell — the primary content region has no text.</summary>
    public static void ShouldHaveMainContent(IWebDriver driver)
    {
        // Same locator ladder as the Playwright suite: semantic main first, then the app's page
        // container, then body as the last resort.
        var main = driver.FindElements(By.CssSelector("main, [role=main], .page, body")).FirstOrDefault(e => e.Displayed);
        if (main is null || main.Text.Trim().Length == 0)
            throw new Xunit.Sdk.XunitException($"Page {driver.Url} rendered a blank shell (no main content).");
    }

    /// <summary>Waits for the page to settle and asserts it didn't bounce to /login (auth held).</summary>
    public static void ShouldBeAuthenticated(IWebDriver driver)
    {
        WaitForBlazor(driver);
        if (driver.Url.Contains("/login", StringComparison.Ordinal))
            throw new Xunit.Sdk.XunitException(
                $"Redirected to /login — session not authenticated ({driver.Url}).");
    }
}
