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
/// App-level host smoke for the FDW <c>StatCardRow</c> dashboard widget, which the reference-ui Home
/// page (<c>/</c>) hosts (nested inside <c>DashboardLayout</c>). The app owns only hosting, so this
/// renders Home with DEFAULT provider contexts and asserts the stat-card row surfaces. The widget's
/// loading/resolved branch coverage was relocated to FDW
/// (<c>Fdw.UI.Components.Blazor.Tests/Components/Dashboard/StatCardRowTests.cs</c>).
/// </summary>
public sealed class StatCardRowTests : BunitContext
{
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
    public void HostedStatCardRowRendersMetricsLandmark()
    {
        HostDefault();
        var cut = Render<Home>();
        // The StatCardRow renders a `.metrics` grid of stat cards with the "Total Pipelines" label
        // when contexts are not loading (the DEFAULT context is not loading).
        cut.FindAll(".metrics").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Total Pipelines");
    }
}
