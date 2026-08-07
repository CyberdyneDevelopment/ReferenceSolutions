using Fdw.Data.Components.DataStores;
using Fdw.UI.Pages.Data.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host tests for the DataStore editor page (the "/datastores/new" create wizard) as
/// the reference-ui app routes to it. These assert ONLY that the app hosts the FDW page and that
/// its landmark renders with a DEFAULT (unseeded) provider context — they do NOT assert the FDW
/// page's internal 3-step wizard, store-type/write-config, container management, or inline
/// new-connection modal behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (DataStoreEditor page tests).
/// </summary>
public sealed class DataStoreEditorPageTests : BunitContext
{
    // Why: swap the live FDW DataStoreEditorProvider for a stub seeded with a DEFAULT context so
    // the page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<DataStoreEditorProvider, DataStoreEditorContext>(new DataStoreEditorContext()));

    [Fact]
    public void HostsDataStoreEditorPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<DataStoreEditorPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDataStoreEditorPageRendersHeaderLandmark()
    {
        HostDefault();
        var cut = Render<DataStoreEditorPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("New DataStore", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
