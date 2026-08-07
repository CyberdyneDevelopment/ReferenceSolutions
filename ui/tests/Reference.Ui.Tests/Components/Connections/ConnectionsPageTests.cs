using Fdw.Services.Connections.Components.Connections;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host tests for the Connections list page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// data/filter/loading behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (ConnectionList component tests).
/// </summary>
public sealed class ConnectionsPageTests : BunitContext
{
    // Why: swap the live FDW ConnectionProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<ConnectionProvider, ConnectionContext>(new ConnectionContext()));

    [Fact]
    public void HostsConnectionsPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Connections.Pages.ConnectionsPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedConnectionsPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Connections.Pages.ConnectionsPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Connections", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
