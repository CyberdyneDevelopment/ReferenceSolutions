using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reference.Ui.Selenium.Infrastructure;

namespace Reference.Ui.Selenium.Pages;

/// <summary>
/// Page Object for the FDW list pages — Selenium port of the Playwright suite's ListPage.
/// The FDW UI ships no <c>data-testid</c> yet and each list page renders a different layout
/// (Connections = card grid, Users = an HTML table). Locators prefer a stable
/// <c>data-testid</c> hook and fall back to the page's current semantic markup — green against
/// today's slot AND auto-upgrades to the test-id contract when FDW components expose it.
/// </summary>
/// <param name="driver">The authenticated browser.</param>
/// <param name="baseUrl">UI root.</param>
/// <param name="route">List route, e.g. <c>/connections</c>.</param>
/// <param name="itemSelector">CSS selecting one list item in this page's current markup
/// (e.g. <c>.grid &gt; .card</c>). The list item is also matched by <c>[data-testid=list-item]</c>.</param>
public sealed class ListPage(IWebDriver driver, string baseUrl, string route, string itemSelector)
{
    private readonly By _itemsBy = By.CssSelector($"[data-testid=list-item], {itemSelector}");

    public void Goto()
    {
        driver.Navigate().GoToUrl($"{baseUrl}{route}");
        PageAssertions.ShouldBeAuthenticated(driver);
        PageAssertions.ShouldNotShowError(driver);
    }

    /// <summary>One row/card per element in the primary list.</summary>
    public IReadOnlyList<IWebElement> Items => driver.FindElements(_itemsBy);

    /// <summary>Waits for the async-loaded list to paint at least one item, then returns the count.</summary>
    public int WaitForItems(int timeoutSeconds = 15)
    {
        new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds))
            .Until(d => d.FindElements(_itemsBy).Any(e => e.Displayed));
        return Items.Count;
    }

    /// <summary>The first list item whose visible text contains <paramref name="text"/>.</summary>
    public IWebElement? ItemContaining(string text) =>
        Items.FirstOrDefault(e => e.Text.Contains(text, StringComparison.OrdinalIgnoreCase));

    /// <summary>The list search/filter input.</summary>
    public IWebElement SearchBox =>
        driver.FindElements(By.CssSelector("[data-testid=search]")).FirstOrDefault()
        ?? driver.FindElement(By.CssSelector(
            "input[placeholder*='earch'], input[placeholder*='ilter']"));

    /// <summary>
    /// Type a query into the search box AND commit it. The FDW search inputs bind with Blazor's
    /// default <c>@bind</c> (onchange — fires on blur), not <c>oninput</c>, so plain SendKeys never
    /// triggers the filter; Tab out to fire the change event, then wait for the circuit to re-render.
    /// </summary>
    public void Search(string text)
    {
        var box = SearchBox;
        box.Clear();
        box.SendKeys(text + Keys.Tab);
        // Why: the re-render is server-round-trip async with no DOM completion marker to wait on;
        // poll until the item set stabilises rather than sleeping a fixed interval.
        var previous = -1;
        new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(d =>
        {
            var current = d.FindElements(_itemsBy).Count;
            var stable = current == previous;
            previous = current;
            return stable;
        });
    }
}
