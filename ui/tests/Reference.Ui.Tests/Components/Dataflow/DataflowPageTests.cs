using Fdw.Operations.Components.Dataflow;
using Reference.Ui.Tests.Infrastructure;
using DataflowPage = Fdw.UI.Pages.Operations.Pages.DataflowPage;

namespace Reference.Ui.Tests.Components.Dataflow;

/// <summary>
/// App-level host tests for the Dataflow overview page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// stat-card / node-grouping / badge / error behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Dataflow page-component tests).
/// </summary>
public sealed class DataflowPageTests : BunitContext
{
    // Why: swap the live FDW DataflowProvider for a stub seeded with a DEFAULT context so the page
    // renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new ProviderFactory<DataflowProvider, DataflowContext>(new DataflowContext()));
    }

    [Fact]
    public void HostsDataflowPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<DataflowPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDataflowPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<DataflowPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Dataflow", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
