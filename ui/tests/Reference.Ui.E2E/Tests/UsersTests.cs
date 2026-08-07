using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// /users admin page, driven through the rendered DOM. The page is a list-CRUD table
/// (<c>table tbody tr</c> per user) with a New User action and an inline create/edit form.
/// <para>
/// CURRENT STATE — the whole <c>/users</c> table is RED: <c>Pages/Users.razor</c> renders each row's
/// avatar initial as <c>@user.Username[0]</c>. <c>UserSummaryPayload.Username</c> is a non-nullable string
/// and the live slot DB contains a user whose Username is the empty string (confirmed via the API:
/// <c>GET /api/v1/users</c> returns a row with <c>"username":""</c>), so <c>Username[0]</c> throws
/// <c>IndexOutOfRangeException</c> inside <c>BuildRenderTree</c>; the table render aborts and the page
/// stays stuck on its loading dots forever (no table, no error banner). The fix is to guard the initial,
/// e.g. <c>user.Username.Length &gt; 0 ? user.Username[0] : '?'</c>.
/// </para>
/// Because the table never paints, the page is un-drivable for create/edit/delete/search through the UI
/// — those flows are blocked by the same crash and documented as gated RED tests. They seed their own
/// known users (unique <c>e2eusr_{8hex}</c> prefix) via the same reference-api the UI uses and assert
/// EXACTLY those, scoped to the prefix, with try/finally teardown.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class UsersTests(PlaywrightFixture fx)
{
    private async Task<(IBrowserContext, ListPage)> OpenUsersAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        var list = new ListPage(page, PlaywrightFixture.BaseUrl, "/users", "table tbody tr");
        await list.GotoAsync();
        return (ctx, list);
    }

    /// <summary>
    /// KNOWN BUG (RED) — the user table must paint at least the rows we seed. It currently never paints
    /// (the empty-username <c>Username[0]</c> crash aborts <c>BuildRenderTree</c>), so the seeded rows
    /// never appear. We seed three known users and assert EXACTLY our three prefix-scoped rows render;
    /// this stays RED until the avatar-initial guard lands. Documents the crash directly.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task TableRendersSeededUsersKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var prefix = ApiSeeder.NewUserPrefix("e2eusr");
        await api.CreateUserAsync($"{prefix}_aaa", $"{prefix}_aaa@x.com");
        await api.CreateUserAsync($"{prefix}_bbb", $"{prefix}_bbb@x.com");
        await api.CreateUserAsync($"{prefix}_ccc", $"{prefix}_ccc@x.com");

        var (ctx, list) = await OpenUsersAsync();
        try
        {
            // The table never paints (IndexOutOfRange in BuildRenderTree); this Wait times out → RED,
            // documenting that the empty-username row aborts the whole table render.
            await list.WaitForItemsAsync(20_000);
            var mine = list.Page.Locator("table tbody tr").Filter(new() { HasTextString = prefix });
            await Assertions.Expect(mine).ToHaveCountAsync(3, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// KNOWN BUG (RED) — search must narrow the table to a single seeded user. Blocked by the same crash
    /// (the table never paints, so the search box filters nothing visible). Seeds one known user and
    /// asserts exactly one prefix-scoped row survives the search.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task SearchNarrowsToSeededUserKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var username = ApiSeeder.NewUserPrefix("e2eusr") + "_srch";
        await api.CreateUserAsync(username, $"{username}@x.com");

        var (ctx, list) = await OpenUsersAsync();
        try
        {
            await list.WaitForItemsAsync(20_000); // RED here: table never paints
            await list.SearchAsync(username);
            var rows = list.Page.Locator("table tbody tr").Filter(new() { HasTextString = username });
            await Assertions.Expect(rows).ToHaveCountAsync(1, new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// KNOWN BUG (RED) — the New User action opens the inline create form. The page is stuck on its
    /// loading state (the table render crashed), so the toolbar/New User button never becomes
    /// interactive; this documents that the create path is unreachable while the table crash stands.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewUserOpensEditorKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenUsersAsync();
        try
        {
            await list.WaitForItemsAsync(20_000); // RED: page stuck on loading dots, table never paints
            await list.NewButton.ClickAsync();
            await Assertions.Expect(list.Page.GetByText(new Regex("create user|new user", RegexOptions.IgnoreCase)).First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(list.Page.Locator("input").First).ToBeVisibleAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// The page route itself responds 2xx (the crash is a client-side render abort, not a server 500):
    /// this GREEN test pins that <c>/users</c> loads authenticated without an HTTP error, isolating the
    /// failure to the table render and keeping it distinct from the access-request prerender 500.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task UsersRouteRespondsOkEvenThoughTableCrashes()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        try
        {
            var resp = await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/users", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            (resp?.Status ?? 0).ShouldBeLessThan(400);
            await PageAssertions.ShouldBeAuthenticatedAsync(page);
            // The shell (New User button) IS present even though the table never paints.
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("New User", RegexOptions.IgnoreCase) }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Opens the inline create form by directly forcing it visible is impossible while the table crash
    /// blocks the New User button from binding; this helper waits for the create panel after a New-User
    /// click and surfaces the same RED gate (the page is stuck on its loading dots).
    /// </summary>
    private static async Task OpenCreateFormAsync(ListPage list)
    {
        await list.WaitForItemsAsync(20_000); // RED gate: table never paints → New User never interactive
        await list.NewButton.ClickAsync();
        await Assertions.Expect(list.Page.GetByText(new Regex("create user|new user", RegexOptions.IgnoreCase)).First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    /// <summary>
    /// bUnit branch <c>Create_WithShortUsername_ShowsValidationError</c> — a username under 3 chars must
    /// surface the client guard "Username must be at least 3 characters" and make NO create call. Reachable
    /// E2E only once the create form opens; currently RED-gated behind the empty-username table crash.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateWithShortUsernameShowsValidationErrorKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenUsersAsync();
        try
        {
            await OpenCreateFormAsync(list);
            await list.Page.GetByPlaceholder("Username").FillAsync("ab");
            await list.Page.GetByPlaceholder(new Regex("Min 8 characters", RegexOptions.IgnoreCase)).First.FillAsync("Passw0rd123!");
            await list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Create$") }).ClickAsync();
            await Assertions.Expect(list.Page.GetByText(new Regex("at least 3 characters", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// bUnit branch <c>Create_WithShortPassword_ShowsValidationError</c> — a password under 8 chars must
    /// surface "Password must be at least 8 characters" and make NO create call. RED-gated behind the
    /// empty-username table crash.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateWithShortPasswordShowsValidationErrorKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, list) = await OpenUsersAsync();
        try
        {
            await OpenCreateFormAsync(list);
            await list.Page.GetByPlaceholder("Username").FillAsync("e2eusr_validname");
            await list.Page.GetByPlaceholder(new Regex("Min 8 characters", RegexOptions.IgnoreCase)).First.FillAsync("short");
            await list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Create$") }).ClickAsync();
            await Assertions.Expect(list.Page.GetByText(new Regex("at least 8 characters", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Valid create through the UI: a unique underscore-only username + valid password creates the user and
    /// the new row appears in the table. RED-gated behind the empty-username crash (the table never paints,
    /// so the row never shows). Self-cleans via the seeder regardless of outcome.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task CreateValidUserAddsRowKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var username = ApiSeeder.NewUserPrefix("e2eusr") + "_crt";
        var (ctx, list) = await OpenUsersAsync();
        try
        {
            await OpenCreateFormAsync(list);
            await list.Page.GetByPlaceholder("Username").FillAsync(username);
            await list.Page.GetByPlaceholder(new Regex("Min 8 characters", RegexOptions.IgnoreCase)).First.FillAsync("Passw0rd123!");
            await list.Page.GetByPlaceholder("Email address").FillAsync($"{username}@x.com");
            await list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Create$") }).ClickAsync();
            api.TrackUserForCleanup(username);
            var row = list.Page.Locator("table tbody tr").Filter(new() { HasTextString = username });
            await Assertions.Expect(row).ToHaveCountAsync(1, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Duplicate username: pre-seed a user, then attempt the same username through the create form. The API
    /// returns "User '{name}' already exists" which surfaces as the form's <c>_formError</c> banner, and NO
    /// second row appears. RED-gated behind the empty-username crash.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateDuplicateUsernameShowsErrorKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var username = ApiSeeder.NewUserPrefix("e2eusr") + "_dup";
        await api.CreateUserAsync(username, $"{username}@x.com");
        var (ctx, list) = await OpenUsersAsync();
        try
        {
            await OpenCreateFormAsync(list);
            await list.Page.GetByPlaceholder("Username").FillAsync(username);
            await list.Page.GetByPlaceholder(new Regex("Min 8 characters", RegexOptions.IgnoreCase)).First.FillAsync("Passw0rd123!");
            await list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Create$") }).ClickAsync();
            await Assertions.Expect(list.Page.GetByText(new Regex("already exists", RegexOptions.IgnoreCase)))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// bUnit branch <c>EditUser_Button_OpensFormInEditMode_PasswordHidden</c> + the full edit round-trip:
    /// seed a user, open its edit form, change Email + toggle Active, Save, reopen the row and read the
    /// fields back. RED-gated behind the empty-username crash (the table never paints so no row is
    /// editable). Documents the complete edit-persist path the UI is meant to support.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task EditUserChangesPersistAcrossReopenKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var username = ApiSeeder.NewUserPrefix("e2eusr") + "_edt";
        await api.CreateUserAsync(username, $"{username}@old.com", active: true);
        var newEmail = $"{username}@new.com";

        var (ctx, list) = await OpenUsersAsync();
        try
        {
            await list.WaitForItemsAsync(20_000); // RED gate: table never paints
            var row = list.Page.Locator("table tbody tr").Filter(new() { HasTextString = username });
            await Assertions.Expect(row).ToHaveCountAsync(1, new() { Timeout = 20_000 });
            // First action button in the row is the edit pencil.
            await row.GetByRole(AriaRole.Button).First.ClickAsync();
            // Password field is hidden in edit mode (bUnit: PasswordHidden).
            await Assertions.Expect(list.Page.GetByText(new Regex("edit user", RegexOptions.IgnoreCase)).First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(list.Page.Locator("input[type=password]")).ToHaveCountAsync(0);
            // Change email + flip Active off.
            var email = list.Page.GetByPlaceholder("Email address");
            await email.FillAsync(newEmail);
            await list.Page.Locator("input[type=checkbox]").First.SetCheckedAsync(false);
            await list.Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Update$") }).ClickAsync();
            await list.Page.WaitForTimeoutAsync(2000);

            // Reopen the row and read back the new email + inactive state.
            await row.GetByRole(AriaRole.Button).First.ClickAsync();
            await Assertions.Expect(list.Page.GetByPlaceholder("Email address"))
                .ToHaveValueAsync(newEmail, new() { Timeout = 10_000 });
            await Assertions.Expect(list.Page.Locator("input[type=checkbox]").First).Not.ToBeCheckedAsync();
        }
        finally { await ctx.CloseAsync(); }
    }

    /// <summary>
    /// Delete a seeded user via the row trash button (no confirm dialog) → the row disappears. RED-gated
    /// behind the empty-username crash. Self-cleans via the seeder as a fallback.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeleteUserRemovesRowKnownBugEmptyUsernameCrash()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var api = await ApiSeeder.CreateAsync();
        var username = ApiSeeder.NewUserPrefix("e2eusr") + "_del";
        await api.CreateUserAsync(username, $"{username}@x.com");

        var (ctx, list) = await OpenUsersAsync();
        try
        {
            await list.WaitForItemsAsync(20_000); // RED gate
            var row = list.Page.Locator("table tbody tr").Filter(new() { HasTextString = username });
            await Assertions.Expect(row).ToHaveCountAsync(1, new() { Timeout = 20_000 });
            // Second action button is the trash/delete (immediate, no confirm).
            await row.GetByRole(AriaRole.Button).Last.ClickAsync();
            await Assertions.Expect(list.Page.Locator("table tbody tr").Filter(new() { HasTextString = username }))
                .ToHaveCountAsync(0, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }
}
