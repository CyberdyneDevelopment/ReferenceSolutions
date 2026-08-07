using Fdw.Services.Messaging.Components.Bell;
using Fdw.Services.SessionState.Components;
using Fdw.UI.Themes;
using Fdw.UI.Themes.Components.Themes;
using Reference.Management.UI.Tailwind.Components.Domain.Tenants;
using Reference.Management.UI.Tailwind.Components.Layout;
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

    [Fact]
    public void RendersSidebarAndAllNavSections()
    {
        RegisterProviderStubs();
        var cut = Render<MainLayout>();
        cut.Markup.ShouldContain("CYBERDYNE");
        // Why this list: these are the NavSections titles the hosted page packages actually
        // contribute. The former list (Schema/Calculations/Lineage/Messaging/Account/System) named
        // sections that no longer exist in Fdw.UI.Registration.NavSections. "Scheduling" is a real
        // section but is deliberately absent: no page in this host's package set declares it, so
        // an empty section renders nothing.
        foreach (var section in new[] { "Data Sources", "Transformations", "Pipelines", "Quality",
                                        "Catalog", "Operations", "Security", "Configuration",
                                        "Developer Tools", "Observability", "Administration" })
            cut.Markup.ShouldContain(section);
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
