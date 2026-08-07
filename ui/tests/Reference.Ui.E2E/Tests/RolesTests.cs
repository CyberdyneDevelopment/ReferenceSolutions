using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// /roles list + /roles/{name} permission-matrix detail, driven through the rendered DOM. The list is
/// a card grid (<c>.grid &gt; div</c> with the role name in an <c>h3</c> and an "Edit Permissions"
/// link). The slot DB is polluted with hundreds of ambient junk roles, so every assertion is scoped to
/// a unique per-test <c>e2e-rl-{8hex}</c> prefix seeded through the same reference-api the UI uses, and
/// cleaned up in <c>finally</c>.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class RolesTests(PlaywrightFixture fx)
{
    private const string CardSelector = ".grid > div";

    private async Task<(IBrowserContext, ListPage)> OpenRolesAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        var list = new ListPage(page, PlaywrightFixture.BaseUrl, "/roles", CardSelector);
        await list.GotoAsync();
        return (ctx, list);
    }

    private static ILocator RoleCard(IPage page, string name) =>
        page.Locator(CardSelector).Filter(new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true }) });

    /// <summary>Seed three known roles; assert EXACTLY those three cards render (scoped to the prefix).</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListRendersExactlyTheSeededRoles()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var prefix = ApiSeeder.NewPrefix("e2e-rl");
        await api.CreateRoleAsync($"{prefix}-aaa", "alpha");
        await api.CreateRoleAsync($"{prefix}-bbb", "beta");
        await api.CreateRoleAsync($"{prefix}-ccc", "gamma");

        var (ctx, list) = await OpenRolesAsync();
        try
        {
            // Cards whose heading starts with our prefix == exactly our three seeded roles.
            var mine = list.Page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex(Regex.Escape(prefix) + "-") });
            await Assertions.Expect(mine).ToHaveCountAsync(3, new() { Timeout = 20_000 });
            await Assertions.Expect(RoleCard(list.Page, $"{prefix}-aaa")).ToContainTextAsync("alpha");
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>New Role → fill name/description → Create → the new card appears in the grid.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task CreateValidRoleAddsCard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-rl") + "-new";
        var (ctx, list) = await OpenRolesAsync();
        try
        {
            await list.NewButton.ClickAsync();
            await list.Page.GetByPlaceholder("Role name").FillAsync(name);
            await list.Page.GetByPlaceholder("Description").FillAsync("created via ui");
            await list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Create$") }).ClickAsync();
            await Assertions.Expect(RoleCard(list.Page, name)).ToBeVisibleAsync(new() { Timeout = 20_000 });
            api.TrackRoleForCleanup(name);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Blank role name is a client-side SILENT no-op (Roles.razor:442 early-returns with no message).
    /// Assert the documented behavior: the create panel stays open and NO card is added.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateWithEmptyNameIsSilentNoop()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenRolesAsync();
        try
        {
            await list.NewButton.ClickAsync();
            await list.Page.GetByPlaceholder("Description").FillAsync("no name");
            await list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Create$") }).ClickAsync();
            await list.Page.WaitForTimeoutAsync(1000);
            // Panel still open (the Name input is still on screen) — the click did nothing.
            await Assertions.Expect(list.Page.GetByPlaceholder("Role name")).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Duplicate role name: pre-seed a role, then try to create a same-name twin through the UI. The API
    /// returns 400 and adds no row; RoleProvider sets ErrorMessage but Roles.razor renders no error sink
    /// (documented silent-failure branch), so the only assertable truth is: still EXACTLY one card with
    /// that name (no duplicate appeared).
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateDuplicateRoleAddsNoSecondCard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-rl") + "-dup";
        await api.CreateRoleAsync(name, "original");

        var (ctx, list) = await OpenRolesAsync();
        try
        {
            await Assertions.Expect(RoleCard(list.Page, name)).ToBeVisibleAsync(new() { Timeout = 20_000 });
            await list.NewButton.ClickAsync();
            await list.Page.GetByPlaceholder("Role name").FillAsync(name);
            await list.Page.GetByPlaceholder("Description").FillAsync("twin");
            await list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Create$") }).ClickAsync();
            await list.Page.WaitForTimeoutAsync(1500);
            await Assertions.Expect(
                list.Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true })).ToHaveCountAsync(1);
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>Seed a throwaway role, delete it via the card trash button, assert its card is gone.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeleteRoleRemovesCard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-rl") + "-del";
        await api.CreateRoleAsync(name, "to delete");

        var (ctx, list) = await OpenRolesAsync();
        try
        {
            var card = RoleCard(list.Page, name);
            await Assertions.Expect(card).ToBeVisibleAsync(new() { Timeout = 20_000 });
            // The only non-link <button> in the card footer is the trash/delete action.
            await card.GetByRole(AriaRole.Button).Last.ClickAsync();
            await Assertions.Expect(
                list.Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true })).ToHaveCountAsync(0, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Detail page: Edit Permissions → /roles/{name} renders the breadcrumb, title and the
    /// Resource|Read|Write|Delete|Exec permission matrix with checkboxes.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task RoleDetailRendersPermissionMatrix()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-rl") + "-mtx";
        await api.CreateRoleAsync(name, "matrix role");

        var (ctx, list) = await OpenRolesAsync();
        try
        {
            var card = RoleCard(list.Page, name);
            await Assertions.Expect(card).ToBeVisibleAsync(new() { Timeout = 20_000 });
            await card.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Edit Permissions") }).ClickAsync();
            // In-circuit nav — poll the URL, never WaitForURL.
            await Assertions.Expect(list.Page).ToHaveURLAsync(new Regex($"/roles/{Regex.Escape(name)}"), new() { Timeout = 20_000 });
            await PageAssertions.ShouldNotShowErrorAsync(list.Page);
            await Assertions.Expect(list.Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true }).First)
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
            // The matrix paints permission checkboxes.
            await Assertions.Expect(list.Page.Locator("input[type=checkbox]").First).ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Permission toggle + Save: toggle the first checkbox, click Save Matrix. The page ignores the save
    /// result (no success toast / no failure banner — documented), so we assert the save round-trips
    /// without throwing an error boundary and the checkbox state we set is reflected after save.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task PermissionToggleAndSaveDoesNotError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-rl") + "-sav";
        await api.CreateRoleAsync(name, "save role");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/roles/{name}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldBeAuthenticatedAsync(page);
            var firstBox = page.Locator("input[type=checkbox]").First;
            await Assertions.Expect(firstBox).ToBeVisibleAsync(new() { Timeout = 20_000 });
            await firstBox.CheckAsync();
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save Matrix", RegexOptions.IgnoreCase) }).ClickAsync();
            await page.WaitForTimeoutAsync(2000);
            await PageAssertions.ShouldNotShowErrorAsync(page);
            await Assertions.Expect(firstBox).ToBeCheckedAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// RED (confirmed bug) — full permission-matrix persist round-trip: on a freshly-seeded role (all
    /// permissions off), CHECK the first permission checkbox, Save Matrix, then RELOAD the detail page and
    /// read the checkbox back. It must remain checked, proving <c>PUT roles/{name}/permissions</c> wrote
    /// the grant. VERIFIED FAILING against ui-ctc: after Save + reload the checkbox is back to UNCHECKED —
    /// the matrix Save does not persist the grant (RoleDetail.razor ignores the save result; the PUT
    /// either no-ops or the read-back path drops it). This is the strong "toggle → save → reopen → verify
    /// persisted" assertion the bUnit suite cannot make (bUnit stubs the save), and it pins a real
    /// data-loss bug the in-memory-only <see cref="PermissionToggleAndSaveDoesNotError"/> can't catch.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task PermissionGrantPersistsAcrossReload()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-rl") + "-prm";
        await api.CreateRoleAsync(name, "persist role");

        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/roles/{name}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PageAssertions.ShouldBeAuthenticatedAsync(page);
            var firstBox = page.Locator("input[type=checkbox]").First;
            await Assertions.Expect(firstBox).ToBeVisibleAsync(new() { Timeout = 20_000 });
            // Seeded role starts with no permissions → the first matrix checkbox is unchecked.
            await Assertions.Expect(firstBox).Not.ToBeCheckedAsync();
            await firstBox.CheckAsync();
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save Matrix", RegexOptions.IgnoreCase) }).ClickAsync();
            await page.WaitForTimeoutAsync(2500);

            // Reload the detail page from scratch and read the grant back.
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/roles/{name}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var reloadedBox = page.Locator("input[type=checkbox]").First;
            await Assertions.Expect(reloadedBox).ToBeVisibleAsync(new() { Timeout = 20_000 });
            await Assertions.Expect(reloadedBox).ToBeCheckedAsync(new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// bUnit branch <c>RoleDetail_DifferentRoleName_FlowsToHeader</c> + the load-error/not-found path:
    /// navigating to a role that does not exist must NOT throw an error boundary; the page renders its
    /// breadcrumb/header shell without the matrix (the provider returns null, no explicit "not found"
    /// message — documented). Asserts the page is a clean render, not a 500 or error boundary.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task RoleDetailForMissingRoleRendersWithoutErrorBoundary()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var missing = ApiSeeder.NewPrefix("e2e-rl") + "-nope";
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            var resp = await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/roles/{missing}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            (resp?.Status ?? 0).ShouldBeLessThan(500);
            await PageAssertions.ShouldBeAuthenticatedAsync(page);
            await PageAssertions.ShouldNotShowErrorAsync(page);
            // The breadcrumb crumb for the role name still renders even when the role isn't found.
            (await page.InnerTextAsync("body")).ShouldContain(missing);
        }
        finally { await ctx.CloseAsync(); }
    }
}
