using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.UI.Components;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host smoke test for the <see cref="ConnectionLimitsEditor"/> FDW component as the
/// reference-ui connection editor hosts it. This asserts ONLY that the app can render the hosted
/// component with DEFAULT (empty) collection parameters and that a host landmark is present — it
/// does NOT assert the component's internal limit-card/value-row/add/remove behaviour. That
/// internal behaviour is covered in Fdw.UI.Components.Blazor.Tests
/// (ConnectionLimitsEditor component tests).
/// </summary>
public sealed class ConnectionLimitsEditorTests : BunitContext
{
    private IRenderedComponent<ConnectionLimitsEditor> HostDefault() =>
        Render<ConnectionLimitsEditor>(p =>
        {
            p.Add(x => x.Limits, (IReadOnlyList<ConnectionLimitConfiguration>)[]);
            p.Add(x => x.LimitTypes, (IReadOnlyList<IConnectionLimitType>)[]);
        });

    [Fact]
    public void HostedConnectionLimitsEditorRendersWithoutThrowing()
    {
        var cut = HostDefault();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedConnectionLimitsEditorRendersHeadingLandmark()
    {
        var cut = HostDefault();
        cut.FindAll("h3").Any(h => h.TextContent.Contains("Connection Limits", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
