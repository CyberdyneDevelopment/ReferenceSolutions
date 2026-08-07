using Fdw.Operations.Components.Dashboard;
using Fdw.Services.Connections.Components.Dashboard;
using Fdw.Services.Pipelines.Components.Dashboard;
using Fdw.Services.Scheduling.Components.Dashboard;
using Fdw.Web.Analytics.Components.Dashboard;
using Fdw.Web.Analytics.Components.Health.Dashboard;
using Reference.Management.UI.Tailwind.Components.Domain.Tenants;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Dashboard;

/// <summary>
/// App-level host smoke for the dashboard. The reference-ui Home page (<c>/</c>) hosts the FDW
/// <c>DashboardLayout</c> composite under six dashboard providers. The app owns only routing and
/// hosting, so this test renders Home with DEFAULT (unseeded) provider contexts and asserts the page
/// composes end-to-end and surfaces the dashboard host landmark. The DashboardLayout's internal
/// branch coverage was relocated to FDW
/// (<c>Fdw.UI.Components.Blazor.Tests/Components/Dashboard/DashboardLayoutTests.cs</c>).
/// </summary>
public sealed class DashboardLayoutTests : BunitContext
{
    // Why: swap every live FDW dashboard provider for a stub seeded with a DEFAULT context so Home
    // renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault()
    {
        ComponentFactories.Add(new ProviderFactory<ConnectionDashboardProvider, ConnectionDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<PipelineDashboardProvider, PipelineDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<ScheduleDashboardProvider, ScheduleDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<AnalyticsDashboardProvider, AnalyticsDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<OperationsDashboardProvider, OperationsDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<HealthDashboardProvider, HealthDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<TenantMultiSelectProvider, TenantMultiSelectContext>(
            new TenantMultiSelectContext()));
    }

    [Fact]
    public void HostsDashboardRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<Home>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDashboardRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<Home>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("System Overview", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
