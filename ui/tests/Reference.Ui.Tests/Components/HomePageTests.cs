using Fdw.Operations.Components.Dashboard;
using Fdw.Services.Connections.Components.Dashboard;
using Fdw.Services.Pipelines.Components.Dashboard;
using Fdw.Services.Scheduling.Components.Dashboard;
using Fdw.Web.Analytics.Components.Dashboard;
using Fdw.Web.Analytics.Components.Health.Dashboard;
using Reference.Ui.Components.Domain.Tenants;
using Reference.Ui.Components.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components;

public sealed class HomePageTests : BunitContext
{
    private void RegisterAllDashboardStubs()
    {
        ComponentFactories.Add(new ProviderFactory<ConnectionDashboardProvider, ConnectionDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<PipelineDashboardProvider, PipelineDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<ScheduleDashboardProvider, ScheduleDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<AnalyticsDashboardProvider, AnalyticsDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<OperationsDashboardProvider, OperationsDashboardContext>());
        ComponentFactories.Add(new ProviderFactory<HealthDashboardProvider, HealthDashboardContext>());
        // The page now mounts a cross-tenant filter bar at the top. Stub its provider so the
        // test doesn't need IHttpClientFactory. Seed IsVisible=false (single-tenant default)
        // so the inner TenantMultiSelect branch never renders.
        ComponentFactories.Add(new ProviderFactory<TenantMultiSelectProvider, TenantMultiSelectContext>(
            new TenantMultiSelectContext()));
        // DashboardLayout itself is a non-provider FDW component; stub it too so we
        // don't pull in deeper widget dependencies just to render the wrappers.
        ComponentFactories.AddStub<Fdw.Dashboard.UI.Components.DashboardLayout>();
    }

    [Fact]
    public void PageRendersWithoutCrashingWithAllProvidersStubbed()
    {
        RegisterAllDashboardStubs();
        var cut = Render<Home>();
        // The stubbed DashboardLayout renders as an empty placeholder; the test confirms
        // every provider in the nested tree resolved and the page composed end-to-end.
        cut.Markup.ShouldNotBeNullOrEmpty();
    }
}
