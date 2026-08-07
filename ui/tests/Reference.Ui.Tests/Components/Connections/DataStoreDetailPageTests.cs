using Fdw.Data.Components.DataStores;
using Fdw.UI.Pages.Data.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host tests for the DataStore drill-down detail page as the reference-ui app routes
/// to it. These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context for a routed Name — they do NOT assert the FDW page's
/// internal loading/error/not-found/overview/path/container/field/tree behaviour. That internal
/// behaviour is covered in Fdw.UI.Components.Blazor.Tests (DataStoreDetail page tests).
/// </summary>
public sealed class DataStoreDetailPageTests : BunitContext
{
    // Why: swap the live FDW DataStoreDetailProvider for a stub seeded with a DEFAULT context so
    // the page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<DataStoreDetailProvider, DataStoreDetailContext>(new DataStoreDetailContext()));

    [Fact]
    public void HostsDataStoreDetailPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<DataStoreDetailPage>(p => p.Add(x => x.Name, "Sales"));
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDataStoreDetailPageRendersRoutedNameLandmark()
    {
        HostDefault();
        var cut = Render<DataStoreDetailPage>(p => p.Add(x => x.Name, "Sales"));
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Sales", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
