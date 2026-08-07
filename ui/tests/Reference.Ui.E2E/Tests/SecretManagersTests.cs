using Microsoft.Playwright;
using Reference.Ui.E2E.Infrastructure;
using Reference.Ui.E2E.Pages;
using Xunit;

namespace Reference.Ui.E2E.Tests;

/// <summary>
/// Secret Managers area, driven through the rendered browser DOM. This page is a single-page
/// list + INLINE create/edit/detail (no separate routes): a card grid (<c>.grid &gt; .card</c>,
/// name in an <c>h3</c>, type badge, EDIT/DELETE per card), an inline create form toggled by the
/// New button, an inline edit dialog (Description only), and an inline detail panel on card click.
/// There is NO search/filter/sort and NO delete-confirm modal (delete is immediate). Fixtures are
/// seeded via <see cref="ApiSeeder"/> and self-clean.
/// </summary>
[Collection("ui-e2e")]
[Trait("Category", "Ui")]
public sealed class SecretManagersTests(PlaywrightFixture fx)
{
    private const string ItemSelector = ".grid > .card";

    private static System.Text.RegularExpressions.Regex Ci(string p) =>
        new(p, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private async Task<(IBrowserContext, IPage)> GotoListAsync()
    {
        var (ctx, page) = await fx.NewAuthenticatedPageAsync();
        await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/secret-managers", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PageAssertions.ShouldBeAuthenticatedAsync(page);
        await PageAssertions.ShouldNotShowErrorAsync(page);
        return (ctx, page);
    }

    private static ILocator CardContaining(IPage page, string text) =>
        page.Locator(ItemSelector).Filter(new() { HasTextString = text });

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListRendersSeededCardsWithTypeAndDescription()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var prefix = ApiSeeder.NewPrefix("e2e-smlist");
        var a = await seeder.CreateSecretManagerAsync($"{prefix}-alpha", "EnvironmentVariable", "alpha desc");
        var b = await seeder.CreateSecretManagerAsync($"{prefix}-bravo", "EnvironmentVariable", "bravo desc");

        var (ctx, page) = await GotoListAsync();
        try
        {
            await page.Locator(ItemSelector).First.WaitForAsync(new() { Timeout = 15_000 });
            await Assertions.Expect(CardContaining(page, a)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            await Assertions.Expect(CardContaining(page, b)).ToHaveCountAsync(1);
            // Card shows type badge + description.
            var aText = await CardContaining(page, a).InnerTextAsync();
            aText.ShouldContain("alpha desc");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task NewButtonRevealsCreateForm()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await GotoListAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("new secret manager") }).ClickAsync();
            // Inline create form (Name input + Type select) appears in-place.
            await Assertions.Expect(page.GetByPlaceholder(Ci("PROD_VAULT"))).ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Bug", "RUI-secretmanager-ui-create-omits-required-configuration")]
    public async Task CreateValidAddsCard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        // CORRECT behavior asserted: filling Name + Type + Description and clicking Create adds a card.
        // RED — CONFIRMED BUG: the create form binds only Name/SecretManagerType/Description on
        // `_createForm` (CreateSecretManagerPayload) and never sets the `Configuration` object, but the
        // server validator requires it ("Configuration object is required"), so the POST 400s and no
        // card is ever added. (Seeding the same shape via the API works because ApiSeeder sends an empty
        // `configuration: {}` object — the UI sends null.)
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = ApiSeeder.NewPrefix("e2e-smnew");
        seeder.TrackSecretManagerForCleanup(name);

        var (ctx, page) = await GotoListAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("new secret manager") }).ClickAsync();
            var nameBox = page.GetByPlaceholder(Ci("PROD_VAULT"));
            await nameBox.FillAsync(name);
            await nameBox.PressAsync("Tab"); // @bind commits on blur — HandleCreate guards on Name
            // Type select — pick Environment Variable (the create-form type select).
            var typeSelect = page.Locator("select").First;
            await typeSelect.SelectOptionAsync(new SelectOptionValue { Value = "EnvironmentVariable" });
            var descBox = page.Locator("textarea").First;
            await descBox.FillAsync("created via UI");
            await descBox.PressAsync("Tab");
            await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^create$") }).ClickAsync();
            // New card appears in the grid (provider reloads list).
            await Assertions.Expect(CardContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CreateCancelHidesFormWithoutCreating()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        var (ctx, page) = await GotoListAsync();
        try
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("new secret manager") }).ClickAsync();
            var nameInput = page.GetByPlaceholder(Ci("PROD_VAULT"));
            await Assertions.Expect(nameInput).ToBeVisibleAsync();
            await nameInput.FillAsync("e2e-sm-should-not-exist");
            // Cancel button inside the create form.
            await page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^cancel$") }).First.ClickAsync();
            // Form closes; the typed name never became a card.
            await Assertions.Expect(nameInput).ToHaveCountAsync(0, new() { Timeout = 10_000 });
            (await CardContaining(page, "e2e-sm-should-not-exist").CountAsync()).ShouldBe(0);
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task EditPersistsDescription()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateSecretManagerAsync(ApiSeeder.NewPrefix("e2e-smedit"),
            "EnvironmentVariable", "orig desc");

        var (ctx, page) = await GotoListAsync();
        try
        {
            await Assertions.Expect(CardContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            // EDIT button on the card opens the inline edit dialog (Description only).
            await CardContaining(page, name).GetByRole(AriaRole.Button, new() { NameRegex = Ci("^edit$") }).ClickAsync();
            // The edit dialog is an inline .card titled "Edit: {name}" (NOT a .fixed modal).
            var editCard = page.Locator(".card").Filter(new() { HasTextString = $"Edit: {name}" });
            await Assertions.Expect(editCard).ToHaveCountAsync(1, new() { Timeout = 10_000 });
            var descBox = editCard.Locator("textarea").First;
            await descBox.FillAsync("edited via UI");
            await descBox.PressAsync("Tab"); // @bind commits on blur
            await editCard.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^save$") }).ClickAsync();

            // Re-open EDIT → the dialog reloads the persisted description.
            await Assertions.Expect(CardContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            await CardContaining(page, name).GetByRole(AriaRole.Button, new() { NameRegex = Ci("^edit$") }).ClickAsync();
            var editCard2 = page.Locator(".card").Filter(new() { HasTextString = $"Edit: {name}" });
            await Assertions.Expect(editCard2).ToHaveCountAsync(1, new() { Timeout = 10_000 });
            (await editCard2.Locator("textarea").First.InputValueAsync()).ShouldBe("edited via UI");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task CardClickOpensDetailPanel()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateSecretManagerAsync(ApiSeeder.NewPrefix("e2e-smdetail"),
            "EnvironmentVariable", "detail desc");

        var (ctx, page) = await GotoListAsync();
        try
        {
            await Assertions.Expect(CardContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            // Clicking the card body (not a button) selects it → inline detail panel.
            await CardContaining(page, name).First.ClickAsync();
            // Detail panel exposes a Close button and the manager's NAME/TYPE block.
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = Ci("^close$") }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            (await page.InnerTextAsync("body")).ShouldContain("EnvironmentVariable");
        }
        finally { await ctx.CloseAsync(); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeleteImmediateRemovesCard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");
        await using var seeder = await ApiSeeder.CreateAsync();
        var name = await seeder.CreateSecretManagerAsync(ApiSeeder.NewPrefix("e2e-smdel"),
            "EnvironmentVariable", "to delete");

        var (ctx, page) = await GotoListAsync();
        try
        {
            await Assertions.Expect(CardContaining(page, name)).ToHaveCountAsync(1, new() { Timeout = 15_000 });
            // No confirm modal — DELETE button removes it immediately.
            await CardContaining(page, name).GetByRole(AriaRole.Button, new() { NameRegex = Ci("^delete$") }).ClickAsync();
            await Assertions.Expect(CardContaining(page, name)).ToHaveCountAsync(0, new() { Timeout = 20_000 });
        }
        finally { await ctx.CloseAsync(); }
    }
}
