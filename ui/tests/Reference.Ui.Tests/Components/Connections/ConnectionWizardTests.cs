using Fdw.Services.Connections.Components.ConnectionWizard;
using Fdw.UI.Pages.Connections.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host tests for the Connection wizard page (the "/connections/new" create flow) as
/// the reference-ui app routes to it. These assert ONLY that the app hosts the FDW page and that
/// its landmark renders with a DEFAULT (unseeded) provider context — they do NOT assert the FDW
/// page's internal step/field/validation/test/save behaviour. That internal behaviour is covered
/// in Fdw.UI.Components.Blazor.Tests (ConnectionWizard page tests).
/// </summary>
public sealed class ConnectionWizardTests : BunitContext
{
    // Why: swap the live FDW ConnectionWizardProvider for a stub seeded with a DEFAULT context so
    // the create wizard renders deterministically without HTTP — the app only owns hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<ConnectionWizardProvider, ConnectionWizardContext>(new ConnectionWizardContext()));

    [Fact]
    public void HostsConnectionWizardPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ConnectionWizardPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedConnectionWizardPageRendersHeaderLandmark()
    {
        HostDefault();
        var cut = Render<ConnectionWizardPage>();
        cut.FindAll("h1").Any(h => h.TextContent.Contains("New Connection", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
