using Fdw.Data.Components.DataStores;
using Fdw.UI.Pages.Data.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host tests for the DataStores list page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW page's internal
/// data/table/delete behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (DataStores page tests).
/// </summary>
public sealed class DataStoresPageTests : BunitContext
{
    // Why: swap the live FDW DataStoreProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<DataStoreProvider, DataStoreContext>(new DataStoreContext()));

    [Fact]
    public void HostsDataStoresPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<DataStoresPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDataStoresPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<DataStoresPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("DataStores", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
