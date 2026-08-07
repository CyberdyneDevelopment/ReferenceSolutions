using Fdw.Services.Connections.Components.Connections;
using Fdw.UI.Pages.Connections.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host tests for the Connection editor page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW page's internal form
/// behaviour (type list, save/create wiring, error banners). That internal behaviour is covered
/// in Fdw.UI.Components.Blazor.Tests (ConnectionEditor page tests).
/// </summary>
public sealed class ConnectionEditorPageTests : BunitContext
{
    // Why: swap the live FDW ConnectionProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<ConnectionProvider, ConnectionContext>(new ConnectionContext()));

    [Fact]
    public void HostsConnectionEditorPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ConnectionEditorPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedConnectionEditorPageRendersHeaderLandmark()
    {
        HostDefault();
        var cut = Render<ConnectionEditorPage>();
        cut.FindAll("h1").Any(h => h.TextContent.Contains("New Connection", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
