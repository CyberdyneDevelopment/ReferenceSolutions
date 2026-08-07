using Fdw.Services.Connections.Components.Connections;
using Fdw.Services.Connections.UI.Components;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host smoke test for the <see cref="ConnectionList"/> FDW component as the
/// reference-ui Connections page hosts it. This asserts ONLY that the app can render the hosted
/// component with a DEFAULT (unseeded) <see cref="ConnectionContext"/> and that a host landmark
/// is present — it does NOT assert the component's internal data/filter/sort/test/delete
/// behaviour. That internal behaviour is covered in Fdw.UI.Components.Blazor.Tests
/// (ConnectionList component tests).
/// </summary>
public sealed class ConnectionListTests : BunitContext
{
    [Fact]
    public void HostedConnectionListRendersWithoutThrowing()
    {
        var cut = Render<ConnectionList>(p => p.Add(x => x.Context, new ConnectionContext()));
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedConnectionListRendersEmptyStateLandmark()
    {
        var cut = Render<ConnectionList>(p => p.Add(x => x.Context, new ConnectionContext()));
        cut.Markup.ShouldContain("No connections configured", Case.Sensitive);
    }
}
