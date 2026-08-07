using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Agent-actions queue (/agent-actions) and the per-action review page (/review/{actionId}).
/// The queue is a table of Pending agent-tool requests; each Pending row has a REVIEW button that
/// routes to /review/{id} where an operator Approves or Denies. Method/Status badges are colour-mapped.
///
/// Covers: queue render (rows vs the "NO_PENDING_ACTIONS" empty branch), the Refresh action, and the
/// review-page not-found branch (ACTION_NOT_FOUND). Approve/Deny against a real Pending row, and the
/// already-reviewed read-only branch, require a seeded Pending agent action; the agent-action create
/// path is an API-internal flow with no UI affordance, so when the slot queue is empty those branches
/// are reached opportunistically (only if a Pending row exists) — see the BLOCKER note on
/// ReviewApprovesFirstPendingAction.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class AgentActionsTests(PlaywrightFixture fx)
{
    private static readonly RegexOptions Ci = RegexOptions.IgnoreCase;

    private async Task<(IBrowserContext, IPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/agent-actions", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    // Settles into one of the two terminal states: rows, or the "NO_PENDING_ACTIONS" empty card.
    private static async Task WaitForQueueSettledAsync(IPage page)
    {
        await Assertions.Expect(
            page.Locator("table tbody tr").First
                .Or(page.GetByText(new Regex("NO_PENDING_ACTIONS", Ci))).First)
            .ToBeVisibleAsync(new() { Timeout = 20_000 });
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task QueueRendersHeaderAndTerminalState()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("Pending Actions", Ci) }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(new Regex("AI Agent Queue", Ci))).ToBeVisibleAsync();
            await WaitForQueueSettledAsync(page);
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task RefreshKeepsQueueHealthy()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await WaitForQueueSettledAsync(page);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("REFRESH", Ci) }).ClickAsync();
            await page.WaitForTimeoutAsync(1200);
            await WaitForQueueSettledAsync(page);
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ReviewButtonRoutesOrQueueIsEmpty()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // REVIEW renders only on Pending rows; if the slot queue is empty this asserts the empty branch
        // instead (NO_PENDING_ACTIONS). Either way the page is healthy and the REVIEW affordance is
        // present exactly when there are pending rows.
        var (ctx, page) = await OpenAsync();
        try
        {
            await WaitForQueueSettledAsync(page);
            var rows = page.Locator("table tbody tr");
            if (await rows.CountAsync() == 0)
            {
                await Assertions.Expect(page.GetByText(new Regex("NO_PENDING_ACTIONS", Ci))).ToBeVisibleAsync();
                return;
            }
            var review = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^REVIEW$", Ci) }).First;
            await review.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/review/\d+"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ReviewUnknownActionShowsNotFound()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // /review/{id} with an id that doesn't exist → CurrentAction null → ACTION_NOT_FOUND branch.
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/review/2147483600",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldBeAuthenticatedAsync(page);
            await Assertions.Expect(page.GetByText(new Regex("ACTION_NOT_FOUND", Ci)))
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ReviewApprovesFirstPendingAction()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // Workflow ACTION: if the queue has a Pending action, drive REVIEW → APPROVE and assert the
        // success transition (navigates back to /agent-actions). The agent-action queue has no UI
        // create path, so when the slot is empty this self-skips and is documented as a BLOCKER:
        // approve/deny success branches cannot be exercised without a seeded Pending agent action.
        var (ctx, page) = await OpenAsync();
        try
        {
            await WaitForQueueSettledAsync(page);
            var review = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^REVIEW$", Ci) });
            if (await review.CountAsync() == 0)
            {
                Assert.Skip("BLOCKER: slot agent-action queue empty (NO_PENDING_ACTIONS) and no UI create " +
                            "path exists — approve/deny success branch unreachable without seeded data.");
                return;
            }
            await review.First.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/review/\d+"), new() { Timeout = 20_000 });
            var approve = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^APPROVE$", Ci) });
            await Assertions.Expect(approve).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await approve.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/agent-actions"), new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }
}
