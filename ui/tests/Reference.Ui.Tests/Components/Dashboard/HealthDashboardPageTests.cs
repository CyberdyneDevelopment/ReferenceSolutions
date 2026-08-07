using Fdw.Web.Analytics.Components.Health.Dashboard;
using Reference.Ui.Tests.Infrastructure;
using HealthPage = Fdw.UI.Pages.Operations.Pages.HealthDashboardPage;

namespace Reference.Ui.Tests.Components.Dashboard;

/// <summary>
/// App-level host smoke for the Health Dashboard page (<c>/health</c>), an FDW page the reference-ui
/// app routes to directly. The app owns only routing/hosting, so this renders the page with a DEFAULT
/// (unseeded) <see cref="HealthDashboardContext"/> and asserts it renders without throwing and
/// surfaces the page host landmark. The page's branch coverage (banner, service map, throughput,
/// uptime formatting, refresh) was relocated to FDW
/// (<c>Fdw.UI.Components.Blazor.Tests/Components/Dashboard/HealthDashboardPageTests.cs</c>).
/// </summary>
public sealed class HealthDashboardPageTests : BunitContext
{
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<HealthDashboardProvider, HealthDashboardContext>(new HealthDashboardContext()));

    [Fact]
    public void HostsHealthDashboardRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<HealthPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedHealthDashboardRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<HealthPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("System Health", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
