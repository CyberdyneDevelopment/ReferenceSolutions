using Fdw.Services.Messaging.Components.Bell;
using Fdw.Services.SessionState.Components;
using Fdw.UI.Themes;
using Fdw.UI.Themes.Components.Themes;
using Reference.Ui.Components.Domain.Tenants;
using Reference.Ui.Components.Layout;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components;

public sealed class MainLayoutTests : BunitContext
{
    private void RegisterProviderStubs(BlazorThemeContext? themeSeed = null, NotificationBellContext? bellSeed = null, SessionStateContext? sessionSeed = null)
    {
        ComponentFactories.Add(new ProviderFactory<BlazorThemeProvider, BlazorThemeContext>(
            themeSeed ?? new BlazorThemeContext { CurrentTheme = new CyberdyneTheme() }));
        ComponentFactories.Add(new ProviderFactory<NotificationBellProvider, NotificationBellContext>(bellSeed));
        ComponentFactories.Add(new ProviderFactory<SessionStateProvider, SessionStateContext>(sessionSeed));
        // The top bar now hosts a ProfileDropdown whose provider needs IHttpClientFactory.
        // Stub the skin component so the layout renders without standing up the tenant HTTP stack.
        ComponentFactories.AddStub<ProfileDropdown>();
    }

    // Why this list: these are the NavSections titles the hosted page packages actually
    // contribute. The former list (Schema/Calculations/Lineage/Messaging/Account/System) named
    // sections that no longer exist in Fdw.UI.Navigation.NavSections. "Scheduling" is a real
    // section but is deliberately absent: no page in this host's package set declares it, so
    // an empty section renders nothing.
    private static readonly string[] ExpectedSections =
    [
        "Data Sources", "Transformations", "Pipelines", "Quality", "Catalog", "Operations",
        "Security", "Configuration", "Developer Tools", "Observability", "Administration",
    ];

    [Fact]
    public void RendersSidebarAndAllNavSectionsForAnAuthenticatedUser()
    {
        // Why the user must be authorized for this to hold: every page in this console declares
        // PageAccess.Authenticated or a permission, so the sections exist only for a caller with a
        // session. Each section above is carried by at least one page declaring plain Authenticated,
        // which is why no permission needs granting here.
        AddAuthorization().SetAuthorized("test-user");
        RegisterProviderStubs();

        var cut = Render<MainLayout>();

        cut.Markup.ShouldContain("CYBERDYNE");
        foreach (var section in ExpectedSections)
            cut.Markup.ShouldContain(section);
    }

    [Fact]
    public void AnonymousVisitorGetsNoNavSections()
    {
        // Why this is the guarantee worth pinning: this test previously rendered with no
        // authentication at all and still expected every section, because a null RequiredPermission
        // meant "anyone" to the nav filter. It now means PageAccess.Authenticated, so a visitor with
        // no session is shown nothing they cannot use. A page that IS public says so with
        // PageAccess.Anonymous; this console declares none, so the sidebar is empty.
        AddAuthorization();
        RegisterProviderStubs();

        var cut = Render<MainLayout>();

        // The chrome still renders — it is the LINKS that are withheld, not the shell.
        cut.Markup.ShouldContain("CYBERDYNE");
        foreach (var section in ExpectedSections)
            cut.Markup.ShouldNotContain(section);
    }

    [Fact]
    public void NotificationBellRendersUnreadBadgeWhenCountGtZero()
    {
        RegisterProviderStubs(bellSeed: new NotificationBellContext { UnreadCount = 5 });
        var cut = Render<MainLayout>();
        cut.Markup.ShouldContain(">5<");
    }

    [Fact]
    public void NotificationBellRenders9PlusWhenOver9()
    {
        RegisterProviderStubs(bellSeed: new NotificationBellContext { UnreadCount = 42 });
        var cut = Render<MainLayout>();
        cut.Markup.ShouldContain("9+");
    }

    [Fact]
    public void NotificationBellHidesBadgeWhenZero()
    {
        RegisterProviderStubs(bellSeed: new NotificationBellContext { UnreadCount = 0 });
        var cut = Render<MainLayout>();
        // Why: the badge is a `.dot` span rendered only when UnreadCount > 0.
        cut.FindAll("span.dot").ShouldBeEmpty();
    }

    [Fact]
    public void SetFullscreenHidesSidebarAndShrinksMain()
    {
        RegisterProviderStubs();
        var cut = Render<MainLayout>();
        cut.Markup.ShouldContain("CYBERDYNE");

        cut.InvokeAsync(() => cut.Instance.SetFullscreen(true));
        cut.Render();

        cut.Markup.ShouldNotContain("CYBERDYNE");
    }

    [Fact]
    public void SetNoPaddingRemovesContentClass()
    {
        RegisterProviderStubs();
        var cut = Render<MainLayout>();
        // Why: with padding the <main> carries the "content" class; SetNoPadding clears it
        // (replaced with an inline flex/overflow style).
        cut.Find("main").ClassList.ShouldContain("content");

        cut.InvokeAsync(() => cut.Instance.SetNoPadding(true));
        cut.Render();

        cut.Find("main").ClassList.ShouldNotContain("content");
    }
}
