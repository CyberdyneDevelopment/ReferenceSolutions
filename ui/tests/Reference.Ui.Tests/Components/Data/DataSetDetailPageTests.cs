using Fdw.Data.Components.Annotations;
using Fdw.Data.Components.DataSets;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Data;

/// <summary>
/// App-level host tests for the DataSet detail page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// tab/field/annotation behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (DataSetDetail component tests).
/// With a default (empty) DataSetContext the page shows "DataSet not found." — that is fine;
/// the host smoke test only verifies the routing landmark and heading render.
/// </summary>
public sealed class DataSetDetailPageTests : BunitContext
{
    // Why: DataSetDetail captures the provider via @ref (typed to the real provider), so use a
    // concrete subclass stub (IS-A DataSetProvider); register infrastructure + seed the nested
    // AnnotationProvider with a DEFAULT context. The app only owns routing/hosting here.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new InheritingProviderFactory<DataSetProvider, StubDataSetProvider>());
        ComponentFactories.Add(new ProviderFactory<AnnotationProvider, AnnotationContext>(new AnnotationContext()));
    }

    [Fact]
    public void HostsDataSetDetailPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Data.Pages.DataSetDetailPage>(p => p
            .AddCascadingValue(new DataSetContextSeed { Value = new DataSetContext() })
            .Add(x => x.Name, "Customers"));
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDataSetDetailPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Data.Pages.DataSetDetailPage>(p => p
            .AddCascadingValue(new DataSetContextSeed { Value = new DataSetContext() })
            .Add(x => x.Name, "Customers"));
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Customers", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
