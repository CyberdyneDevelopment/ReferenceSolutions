using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Access-requests area, driven through the rendered DOM. Covers the list page (filter chips +
/// approve/deny actions) and the standalone <c>/access-requests/new</c> create form. The list page is
/// currently RED: its <c>MessageProvider</c> auto-loads in <c>OnInitializedAsync</c> and mutates
/// component state off the Blazor Dispatcher during prerender → HTTP 500 (the create form does not
/// auto-load, so it renders fine). Mutating tests seed their own pending requests via the same
/// reference-api the UI uses and clean nothing here (approve/deny is terminal) — but they are gated
/// behind the list page rendering at all.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class AccessRequestsTests(PlaywrightFixture fx)
{
    /// <summary>
    /// KNOWN BUG (RED) — <c>/access-requests</c> returns HTTP 500. Root cause:
    /// <c>Fdw.Services.Messaging.Components.MessageProvider</c> (wrapped with
    /// <c>AutoLoadAccessRequests="true"</c>) calls <c>LoadAccessRequests()</c> from
    /// <c>OnInitializedAsync</c> during the server prerender pass and calls <c>StateHasChanged()</c> off
    /// the Blazor Dispatcher → <c>InvalidOperationException</c> → 500. Every other provider loads in
    /// <c>OnAfterRenderAsync(firstRender)</c> (interactive only), which is why only the Message-backed
    /// pages 500. This asserts the CORRECT behavior (the page must render with a 2xx), so it stays RED
    /// until the provider marshals its load onto the Dispatcher / defers to first interactive render.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListPageRendersKnownBugPrerender500()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            var resp = await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/access-requests", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            (resp?.Status ?? 0).ShouldBeLessThan(500,
                "/access-requests 500s: MessageProvider mutates state off the Blazor Dispatcher during prerender (known bug).");
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// When the list renders (i.e. once the prerender bug is fixed), a seeded pending request must appear
    /// with Approve/Deny actions. Until then this is gated behind the same 500 and stays RED. Scoped to a
    /// unique resource string so it never asserts against ambient rows.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task SeededPendingRequestAppearsWithActionsKnownBugPrerender500()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var resource = string.Concat($"e2e-ar-{Guid.NewGuid():N}".AsSpan(0, 13), ":Conn");
        await api.CreateAccessRequestAsync(resource, "Read", "e2e justification");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            var resp = await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/access-requests", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            (resp?.Status ?? 0).ShouldBeLessThan(500, "/access-requests 500s during prerender (known bug).");

            var row = page.Locator("table tbody tr").Filter(new() { HasTextString = resource });
            await Assertions.Expect(row).ToHaveCountAsync(1, new() { Timeout = 20_000 });
            await Assertions.Expect(row.GetByRole(AriaRole.Button, new() { Name = "Approve" })).ToBeVisibleAsync();
            await Assertions.Expect(row.GetByRole(AriaRole.Button, new() { Name = "Deny" })).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Approve flow (RED, gated on the prerender 500): seed a pending request, open the list, click its
    /// Approve action, and assert the row's status badge flips to Approved (provider reloads on success).
    /// Blocked today by the MessageProvider prerender 500 — asserts the CORRECT post-approve behavior so it
    /// goes green once the list renders. Approve is terminal (no cleanup needed).
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task ApprovePendingRequestFlipsStatusKnownBugPrerender500()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var resource = string.Concat($"e2e-ar-{Guid.NewGuid():N}".AsSpan(0, 13), ":Apr");
        await api.CreateAccessRequestAsync(resource, "Read", "approve me");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            var resp = await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/access-requests", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            (resp?.Status ?? 0).ShouldBeLessThan(500, "/access-requests 500s during prerender (known bug).");

            var row = page.Locator("table tbody tr").Filter(new() { HasTextString = resource });
            await Assertions.Expect(row).ToHaveCountAsync(1, new() { Timeout = 20_000 });
            await row.GetByRole(AriaRole.Button, new() { Name = "Approve" }).ClickAsync();
            await Assertions.Expect(
                page.Locator("table tbody tr").Filter(new() { HasTextString = resource }).GetByText(new Regex("Approved", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Deny flow (RED, gated on the prerender 500): seed a pending request, open the deny modal, enter a
    /// reason, confirm Deny, and assert the row's status flips to Denied. Asserts the CORRECT behavior;
    /// blocked today by the same prerender 500.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task DenyPendingRequestViaModalFlipsStatusKnownBugPrerender500()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var resource = string.Concat($"e2e-ar-{Guid.NewGuid():N}".AsSpan(0, 13), ":Dny");
        await api.CreateAccessRequestAsync(resource, "Read", "deny me");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            var resp = await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/access-requests", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            (resp?.Status ?? 0).ShouldBeLessThan(500, "/access-requests 500s during prerender (known bug).");

            var row = page.Locator("table tbody tr").Filter(new() { HasTextString = resource });
            await Assertions.Expect(row).ToHaveCountAsync(1, new() { Timeout = 20_000 });
            await row.GetByRole(AriaRole.Button, new() { Name = "Deny" }).ClickAsync();
            // The deny modal opens with a reason textarea.
            await page.GetByPlaceholder(new Regex("Reason for denial", RegexOptions.IgnoreCase)).FillAsync("e2e deny reason");
            await page.GetByRole(AriaRole.Button, new() { Name = "Deny", Exact = true }).Last.ClickAsync();
            await Assertions.Expect(
                page.Locator("table tbody tr").Filter(new() { HasTextString = resource }).GetByText(new Regex("Denied", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Filter chips (RED, gated on the prerender 500): seed a pending request, then click the "Pending"
    /// chip and assert the seeded row is present; click "Approved" and assert it disappears (client-side
    /// status filter). Asserts CORRECT filter behavior; blocked today by the prerender 500.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task StatusFilterChipsNarrowListKnownBugPrerender500()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var resource = string.Concat($"e2e-ar-{Guid.NewGuid():N}".AsSpan(0, 13), ":Flt");
        await api.CreateAccessRequestAsync(resource, "Read", "filter me");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            var resp = await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/access-requests", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            (resp?.Status ?? 0).ShouldBeLessThan(500, "/access-requests 500s during prerender (known bug).");

            await page.GetByRole(AriaRole.Button, new() { Name = "Pending", Exact = true }).ClickAsync();
            await Assertions.Expect(page.Locator("table tbody tr").Filter(new() { HasTextString = resource }))
                .ToHaveCountAsync(1, new() { Timeout = 20_000 });
            await page.GetByRole(AriaRole.Button, new() { Name = "Approved", Exact = true }).ClickAsync();
            await Assertions.Expect(page.Locator("table tbody tr").Filter(new() { HasTextString = resource }))
                .ToHaveCountAsync(0, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    // ---- /access-requests/new (renders fine — no auto-load) ---------------------------------------

    private async Task<(IBrowserContext, IPage)> OpenNewAsync(string query = "")
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/access-requests/new{query}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    /// <summary>The create form renders its Resource / Permission / Justification inputs and Submit.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewFormRenders()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenNewAsync();
        try
        {
            await Assertions.Expect(page.GetByPlaceholder(new Regex("Connection:ProductionDb"))).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await Assertions.Expect(page.GetByPlaceholder(new Regex("Read, Write, Execute"))).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Submit Request" })).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>Submitting with empty Resource/Permission shows the required-fields error and makes no call.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewFormEmptySubmitShowsRequiredError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenNewAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Submit Request" }).ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("Resource and Permission are required")))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            // No navigation occurred — still on /new.
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/access-requests/new"));
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Missing-only-Permission branch: filling Resource alone still trips the same required-fields guard
    /// (the guard is <c>IsNullOrWhiteSpace(_resource) || IsNullOrWhiteSpace(_permission)</c>).
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task NewFormResourceOnlyShowsRequiredError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenNewAsync();
        try
        {
            await page.GetByPlaceholder(new Regex("Connection:ProductionDb")).FillAsync("e2e:OnlyResource");
            await page.GetByRole(AriaRole.Button, new() { Name = "Submit Request" }).ClickAsync();
            await Assertions.Expect(page.GetByText(new Regex("Resource and Permission are required")))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Valid submit: fill Resource + Permission + Justification, submit → POST access-requests succeeds →
    /// the page navigates away from <c>/new</c> (to <c>/messages</c>). Uses a unique resource so the
    /// created request is self-identifying; it is left in the queue (creation has no UI delete path).
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewFormValidSubmitNavigatesAway()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenNewAsync();
        try
        {
            var resource = string.Concat($"e2e-ar-{Guid.NewGuid():N}".AsSpan(0, 13), ":New");
            await page.GetByPlaceholder(new Regex("Connection:ProductionDb")).FillAsync(resource);
            await page.GetByPlaceholder(new Regex("Read, Write, Execute")).FillAsync("Read");
            await page.GetByPlaceholder(new Regex("Explain why you need")).FillAsync("e2e valid submit");
            await page.GetByRole(AriaRole.Button, new() { Name = "Submit Request" }).ClickAsync();
            // In-circuit nav off /new on success (lands on /messages). Poll until the URL no longer ends /new.
            await Assertions.Expect(page).ToHaveURLAsync(
                new Regex(@"^(?!.*/access-requests/new$).*$"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(page);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>Query prefill: <c>?resource=&amp;permission=</c> populate the inputs on load.</summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task NewFormPrefillsFromQuery()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await OpenNewAsync("?resource=Connection:Prefilled&permission=Write");
        try
        {
            await Assertions.Expect(page.GetByPlaceholder(new Regex("Connection:ProductionDb")))
                .ToHaveValueAsync("Connection:Prefilled", new() { Timeout = 10_000 });
            await Assertions.Expect(page.GetByPlaceholder(new Regex("Read, Write, Execute")))
                .ToHaveValueAsync("Write");
        }
        finally { await ctx.CloseAsync(); }
    }
}
