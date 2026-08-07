using Microsoft.Extensions.Logging;

// Why: Fdw.UI.Registration.NavItem (the DECLARATION) and Components.Layout.NavItem (the COMPONENT that
// renders it) share a name. Aliasing the declaration side keeps bare `NavItem` meaning the component,
// which is what every Render<T> below is about.
using Registration = Fdw.UI.Registration;

namespace Reference.Ui.Tests.Components;

public sealed class NavItemTests : BunitContext
{
    // Why: the component takes a PAGE, not an href — it resolves the link from the [Route] attribute on
    // the page's component type (what @page compiles to). A test therefore needs real routed component
    // types; an href string is no longer expressible.
    [Route("/connectors")]
    private sealed class ConnectorsPage : ComponentBase;

    [Route("/")]
    private sealed class HomePage : ComponentBase;

    [Route("/other")]
    private sealed class OtherPage : ComponentBase;

    // Why: a parameterised template cannot be a nav target, so a page declaring ONLY one has no
    // resolvable link — the failure branch below.
    [Route("/datasets/{Name}/edit")]
    private sealed class ParameterisedOnlyPage : ComponentBase;

    private sealed class TestPage(Type component, string label, string icon) : Registration.IPage
    {
        public string Name => "test-page";

        public Type Component { get; } = component;

        public Registration.INavItem NavItem { get; } = new Registration.NavItem(label, icon, null, 0);

        // Why Authenticated rather than Anonymous: these tests render the nav ITEM directly, never through
        // NavTree's filter, so the value is not exercised — and the honest stand-in for a console page is
        // the rule every page in this console actually declares.
        public Registration.IPageAccess Access => Registration.PageAccess.Authenticated;
    }

    // Why: the component injects ILogger<NavItem> so a bad declaration is reported rather than thrown;
    // without logging registered the render fails on the injection, not on the behaviour under test.
    public NavItemTests() => Services.AddLogging();

    private void Navigate(string relative)
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo(relative);
    }

    private IRenderedComponent<NavItem> RenderNav(
        Type component, string label = "Connectors", string icon = "database")
        => Render<NavItem>(p => p.Add(x => x.Page, new TestPage(component, label, icon)));

    [Fact]
    public void RendersHrefAndText()
    {
        var cut = RenderNav(typeof(ConnectorsPage));

        var a = cut.Find("a");
        a.GetAttribute("href").ShouldBe("/connectors");
        a.TextContent.ShouldContain("Connectors");
    }

    [Fact]
    public void NotActiveOnDifferentPath()
    {
        Navigate("/other");
        var cut = RenderNav(typeof(ConnectorsPage));

        // Why: GetNavClass renders "nav-item active" (space-separated) when active; the
        // inactive class list is just "nav-item".
        cut.Find("a").ClassList.ShouldNotContain("active");
    }

    [Fact]
    public void ActiveWhenCurrentPathMatchesHrefPrefix()
    {
        Navigate("/connectors/123");
        var cut = RenderNav(typeof(ConnectorsPage));

        cut.Find("a").ClassList.ShouldContain("active");
    }

    [Fact]
    public void RootHrefActiveOnEmptyPath()
    {
        var cut = RenderNav(typeof(HomePage), label: "Home");

        cut.Find("a").ClassList.ShouldContain("active");
    }

    [Fact]
    public void RootHrefNotActiveOnAnotherPath()
    {
        // Why: "/" is a prefix of every path, so the root entry needs the empty-path special case to
        // avoid being permanently active. This is the guard for that branch.
        Navigate("/other");
        var cut = RenderNav(typeof(HomePage), label: "Home");

        cut.Find("a").ClassList.ShouldNotContain("active");
    }

    [Fact]
    public void PageWithNoParameterlessRouteRendersDisabledInPlace()
    {
        // Why: a page that declares a sidebar entry but no route it can link to is a declaration error.
        // It must render in place, marked and unreachable, rather than throw and tear the whole sidebar
        // down, and rather than vanish silently.
        var cut = RenderNav(typeof(ParameterisedOnlyPage), label: "Calculated");

        cut.FindAll("a").ShouldBeEmpty();
        var span = cut.Find("span");
        span.ClassList.ShouldContain("nav-item-broken");
        span.TextContent.ShouldContain("Calculated");
        span.TextContent.ShouldContain("unreachable");
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("workflow")]
    [InlineData("database")]
    [InlineData("server")]
    [InlineData("table")]
    [InlineData("clock")]
    [InlineData("calculator")]
    [InlineData("git-branch")]
    [InlineData("arrows")]
    [InlineData("eye")]
    [InlineData("shield")]
    [InlineData("settings")]
    [InlineData("palette")]
    [InlineData("mail")]
    [InlineData("key")]
    [InlineData("bell")]
    [InlineData("user")]
    [InlineData("terminal")]
    [InlineData("unknown-icon")]
    public void RendersAnSvgForEveryIcon(string icon)
    {
        var cut = RenderNav(typeof(OtherPage), icon: icon);

        cut.Find("svg path").GetAttribute("d").ShouldNotBeNullOrEmpty();
    }
}
