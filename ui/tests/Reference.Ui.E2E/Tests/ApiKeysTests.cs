using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// /api-keys, driven through the rendered DOM. Two inline-CRUD sections — Personal Access Tokens and
/// Agent Keys — each with a New button, a create panel (Label + Expires) and a per-row Revoke/Delete
/// action. The current API build does NOT expose the <c>users/me/tokens</c> / <c>agent-keys</c> routes
/// (they 404), so the lists are empty and a create attempt surfaces the page's error banner — both are
/// deterministic, asserted behaviors here (no ambient data involved).
/// <para>
/// bUnit-only branches (physically unreachable E2E while the backend 404s — no row can ever exist):
/// <c>RevokeToken_InvokesOnRevokeToken_WithTokenId</c>, <c>DeleteAgentKey_InvokesOnDeleteAgentKey_WithKeyId</c>,
/// <c>List_RendersTokenAndAgentKeyLabels</c>, and <c>NewTokenValue_RendersCopyBanner_AndValue</c> (the
/// reveal-once "Copy this token now" banner only appears after a successful create, which 404s here).
/// Those are covered by bUnit with a stubbed provider; this file pins the masked-failure UX instead.
/// </para>
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class ApiKeysTests(PlaywrightFixture fx)
{
    private async Task<(IBrowserContext, IPage)> OpenAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/api-keys", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        await page.WaitForTimeoutAsync(2500); // interactive list load
        return (ctx, page);
    }

    /// <summary>Both sections render their headers and their (empty) section bodies.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task BothSectionsRender()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Personal Access Tokens" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Agent Keys", Exact = true })).ToBeVisibleAsync();
            // No token/agent-key backend on this build → documented empty-state text for each section.
            await Assertions.Expect(page.GetByText("No personal access tokens")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("No agent keys")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>New Token reveals the create panel with the Label input.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewTokenOpensCreatePanel()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "New Token" }).First.ClickAsync();
            await Assertions.Expect(page.GetByPlaceholder("e.g., CI/CD Pipeline")).ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Empty Label is a client-side SILENT no-op (SubmitCreateToken early-returns, no message): clicking
    /// Create with a blank label neither errors nor adds a row — the panel just stays open.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateTokenWithEmptyLabelIsSilentNoop()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "New Token" }).First.ClickAsync();
            await Assertions.Expect(page.GetByPlaceholder("e.g., CI/CD Pipeline")).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            // Panel stays open (silent early-return) and no error banner appears for the empty-label path.
            await Assertions.Expect(page.GetByPlaceholder("e.g., CI/CD Pipeline")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(new Regex("Failed to create personal access token"))).ToHaveCountAsync(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Create token with a valid label: the <c>POST users/me/tokens</c> route is absent on this API build
    /// (404), so the provider's failure path fires and the page shows the "Failed to create personal
    /// access token" error banner with NO new token row and NO "copy this token now" reveal. This asserts
    /// the masked-failure UX (generic banner + no row), documenting the missing token backend.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateTokenSurfacesErrorBannerAndAddsNoRow()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "New Token" }).First.ClickAsync();
            await page.GetByPlaceholder("e.g., CI/CD Pipeline").FillAsync("e2e-tok-throwaway");
            await page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).First.ClickAsync();
            await page.WaitForTimeoutAsync(3000);

            await Assertions.Expect(page.GetByText(new Regex("Failed to create personal access token")))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            // No raw-token reveal, list stays empty.
            await Assertions.Expect(page.GetByText(new Regex("Copy this token now", RegexOptions.IgnoreCase))).ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByText("No personal access tokens")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>New Agent Key reveals the agent-key create panel.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewAgentKeyOpensCreatePanel()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "New Agent Key" }).First.ClickAsync();
            await Assertions.Expect(page.GetByPlaceholder("e.g., Claude Agent")).ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Empty agent-key Label is a client-side SILENT no-op (SubmitCreateAgentKey early-returns): clicking
    /// Create with a blank label neither errors nor adds a row — the panel just stays open. Mirrors the
    /// token blank-label branch for the Agent Keys section.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateAgentKeyWithEmptyLabelIsSilentNoop()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "New Agent Key" }).First.ClickAsync();
            await Assertions.Expect(page.GetByPlaceholder("e.g., Claude Agent")).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            await Assertions.Expect(page.GetByPlaceholder("e.g., Claude Agent")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(new Regex("Failed to create agent key"))).ToHaveCountAsync(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Create agent key with a valid label: the <c>POST agent-keys</c> route is absent on this API build
    /// (404), so the provider's failure path fires and the page shows the "Failed to create agent key"
    /// error banner with NO new key row and NO reveal banner. Mirrors the token masked-failure test and
    /// documents the missing agent-key backend.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateAgentKeySurfacesErrorBannerAndAddsNoRow()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "New Agent Key" }).First.ClickAsync();
            await page.GetByPlaceholder("e.g., Claude Agent").FillAsync("e2e-agt-throwaway");
            await page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).First.ClickAsync();
            await page.WaitForTimeoutAsync(3000);

            await Assertions.Expect(page.GetByText(new Regex("Failed to create agent key")))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await Assertions.Expect(page.GetByText(new Regex("Copy this token now", RegexOptions.IgnoreCase))).ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByText("No agent keys")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>Refresh reloads both lists without an error boundary.</summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task RefreshReloadsWithoutError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await Assertions.Expect(page.GetByText("No personal access tokens")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }
}
