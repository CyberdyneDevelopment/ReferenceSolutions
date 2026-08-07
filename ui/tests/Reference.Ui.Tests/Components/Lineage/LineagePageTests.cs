using Fdw.Operations.Components.Dataflow;
using Fdw.Operations.Components.Lineage;
using Reference.Ui.Tests.Infrastructure;
using LineagePage = Fdw.UI.Pages.Operations.Pages.LineagePage;

namespace Reference.Ui.Tests.Components.Lineage;

/// <summary>
/// App-level host tests for the Lineage page as the reference-ui app routes to it. These assert
/// ONLY that the app hosts the FDW page and that its landmark renders with DEFAULT (unseeded)
/// provider contexts — they do NOT assert the FDW component's internal toolbar/graph-build/node-
/// selection/zoom behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Lineage page-component tests).
/// </summary>
public sealed class LineagePageTests : BunitContext
{
    // Why: the page nests a LineageProvider and a DataflowProvider; swap BOTH for stubs seeded with
    // DEFAULT contexts so the page renders deterministically without HTTP. The page itself injects
    // IHttpClientFactory + NavigationManager and builds named clients in OnInitialized, so the
    // shared provider infrastructure (a no-op IHttpClientFactory + ILoggerFactory) must be present.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new ProviderFactory<LineageProvider, LineageContext>(new LineageContext()));
        ComponentFactories.Add(new ProviderFactory<DataflowProvider, DataflowContext>(new DataflowContext()));
    }

    [Fact]
    public void HostsLineagePageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<LineagePage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedLineagePageRendersGraphLandmark()
    {
        HostDefault();
        var cut = Render<LineagePage>();
        cut.FindAll(".graph").Count.ShouldBeGreaterThan(0);
        cut.FindAll(".glabel").Any(e => e.TextContent.Contains("System Lineage", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
