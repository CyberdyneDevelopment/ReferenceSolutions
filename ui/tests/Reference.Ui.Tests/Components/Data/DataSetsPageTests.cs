using Fdw.Data.Components.DataSets;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Data;

/// <summary>
/// App-level host tests for the DataSets list page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// data/filter/loading behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (DataSets component tests).
/// </summary>
public sealed class DataSetsPageTests : BunitContext
{
    // Why: swap the live FDW DataSetProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<DataSetProvider, DataSetContext>(new DataSetContext()));

    [Fact]
    public void HostsDataSetsPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Data.Pages.DataSetsPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDataSetsPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Data.Pages.DataSetsPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("DataSets", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
