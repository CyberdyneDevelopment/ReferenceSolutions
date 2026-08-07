using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using System.Text.RegularExpressions;

namespace Reference.Ui.E2E.Pages;

/// <summary>
/// Page Object for the table-based FDW list pages — Pipelines, Schedules, Calculations all render a
/// <c>table.table</c> with a <c>tbody &gt; tr.group</c> per row and the entity name in the first cell.
/// Unlike <see cref="ListPage"/> (card / divided-list layouts) these pages key off the table row and
/// expose per-row action buttons addressed by their <c>title</c> attribute (Execute/Edit/Delete/Pause).
/// None of these pages renders a search box (the providers carry filter logic but no input is wired in
/// the markup), so search/filter coverage asserts that fact rather than driving a control.
/// </summary>
/// <param name="page">The authenticated browser page.</param>
/// <param name="baseUrl">UI root.</param>
/// <param name="route">List route, e.g. <c>/pipelines</c>.</param>
public sealed class AreaTablePage(IPage page, string baseUrl, string route)
{
    public IPage Page => page;
    public string BaseUrl => baseUrl;
    public string Route => route;

    /// <summary>Navigate to the list route and assert it loaded authenticated with no error boundary.</summary>
    public async Task GotoAsync()
    {
        await page.GotoAsync($"{baseUrl}{route}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
    }

    /// <summary>All data rows in the list table.</summary>
    public ILocator Rows => page.Locator("table.table tbody tr");

    /// <summary>The list table itself (absent when the list is empty and only the empty-state shows).</summary>
    public ILocator Table => page.Locator("table.table");

    /// <summary>The row whose visible text contains <paramref name="text"/> (e.g. a unique entity name).</summary>
    public ILocator RowContaining(string text) => Rows.Filter(new() { HasTextString = text });

    /// <summary>The toolbar New/Create action (link on Pipelines/Schedules, button on Calculations).</summary>
    public ILocator NewButton => page
        .GetByRole(AriaRole.Link, new() { NameRegex = new Regex("new", RegexOptions.IgnoreCase) })
        .Or(page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("new", RegexOptions.IgnoreCase) }))
        .First;

    /// <summary>Waits for the async circuit data load to paint at least one row, returns the count.
    /// Returns 0 (without throwing) when the empty-state renders instead.</summary>
    public async Task<int> WaitForRowsOrEmptyAsync(int timeoutMs = 15_000)
    {
        // Race the first row against the empty-state card — whichever the circuit paints first wins.
        try
        {
            await Rows.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs,
            });
            return await Rows.CountAsync();
        }
        catch (TimeoutException)
        {
            return 0;
        }
    }
}
