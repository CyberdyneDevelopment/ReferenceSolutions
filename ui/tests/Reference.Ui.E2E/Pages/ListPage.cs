using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using System.Text.RegularExpressions;

namespace Reference.Ui.E2E.Pages;

/// <summary>
/// Page Object for the FDW list pages. The FDW UI ships no <c>data-testid</c> yet and each list page
/// renders a different layout (Connections = card grid, DataSets = a divided-list card of row buttons,
/// Users = an HTML table). So each locator prefers a stable <c>data-testid</c> hook and falls back to
/// the page's current semantic markup — the suite is green against today's slot AND auto-upgrades to
/// the test-id contract the moment FDW components expose it, with no test change.
/// </summary>
/// <param name="page">The authenticated browser page.</param>
/// <param name="baseUrl">UI root.</param>
/// <param name="route">List route, e.g. <c>/connections</c>.</param>
/// <param name="itemSelector">CSS selecting one list item in this page's current markup
/// (e.g. <c>.grid &gt; .card</c>). The list item is also matched by <c>[data-testid=list-item]</c>.</param>
public sealed class ListPage(IPage page, string baseUrl, string route, string itemSelector)
{
    private static readonly RegexOptions Ci = RegexOptions.IgnoreCase;

    public IPage Page => page;

    public async Task GotoAsync()
    {
        await page.GotoAsync($"{baseUrl}{route}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
    }

    /// <summary>One row/card in the primary list.</summary>
    public ILocator Items => page.Locator($"[data-testid=list-item], {itemSelector}");

    /// <summary>Waits for the async-loaded list to paint at least one item, then returns the count.</summary>
    public async Task<int> WaitForItemsAsync(int timeoutMs = 15_000)
    {
        await Items.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
        return await Items.CountAsync();
    }

    public Task<int> ItemCountAsync() => Items.CountAsync();

    /// <summary>The list item whose visible text contains <paramref name="text"/>.</summary>
    public ILocator ItemContaining(string text) => Items.Filter(new() { HasTextString = text });

    /// <summary>The toolbar New/Add/Create action (button or link).</summary>
    public ILocator NewButton => page.Locator("[data-testid=new-button]")
        .Or(page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("new|add|create", Ci) }))
        .Or(page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("new|add|create", Ci) }))
        .First;

    /// <summary>The list search/filter input.</summary>
    public ILocator SearchBox => page.Locator("[data-testid=search]")
        .Or(page.GetByPlaceholder(new Regex("search|filter", Ci)))
        .First;

    /// <summary>
    /// Type a query into the search box AND commit it. The FDW search inputs bind with Blazor's default
    /// <c>@bind</c> (onchange — fires on blur), not <c>oninput</c>, so a plain Fill never triggers the
    /// filter; we Tab out to fire the change event, then let the circuit re-render.
    /// </summary>
    public async Task SearchAsync(string text)
    {
        await SearchBox.FillAsync(text);
        await SearchBox.PressAsync("Tab");
        await page.WaitForTimeoutAsync(600);
    }
}
