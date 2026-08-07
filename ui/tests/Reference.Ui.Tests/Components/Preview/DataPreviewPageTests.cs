using Fdw.Data.UI.Components;
using Reference.Ui.Tests.Infrastructure;
using DataPreviewPage = Fdw.UI.Pages.Data.Pages.DataPreviewPage;

namespace Reference.Ui.Tests.Components.Preview;

/// <summary>
/// App-level host tests for the Data Preview page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// query/visualization behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (DataPreview component tests).
/// </summary>
public sealed class DataPreviewPageTests : BunitContext
{
    // Why: swap the live FDW DataPreviewPageProvider for a stub seeded with a DEFAULT context so
    // the page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<DataPreviewPageProvider, DataPreviewPageContext>(new DataPreviewPageContext()));

    [Fact]
    public void HostsDataPreviewPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<DataPreviewPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDataPreviewPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<DataPreviewPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Data Preview", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
