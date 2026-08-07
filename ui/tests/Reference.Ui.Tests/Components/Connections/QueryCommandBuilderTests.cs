using System.Collections.Generic;
using Fdw.Services.Connections.UI.Components;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host smoke test for the <see cref="QueryCommandBuilder"/> FDW component as the
/// reference-ui task editor hosts it. This asserts ONLY that the app can render the hosted
/// component with a DEFAULT (empty) configuration dictionary and that a host landmark is present
/// — it does NOT assert the component's internal container/field/filter/sort/paging/hydration
/// behaviour. That internal behaviour is covered in Fdw.UI.Components.Blazor.Tests
/// (QueryCommandBuilder component tests).
/// </summary>
public sealed class QueryCommandBuilderTests : BunitContext
{
    private IRenderedComponent<QueryCommandBuilder> HostDefault() =>
        Render<QueryCommandBuilder>(p =>
            p.Add(x => x.TaskConfiguration, (IDictionary<string, object?>)new Dictionary<string, object?>(StringComparer.Ordinal)));

    [Fact]
    public void HostedQueryCommandBuilderRendersWithoutThrowing()
    {
        var cut = HostDefault();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedQueryCommandBuilderRendersContainerLandmark()
    {
        var cut = HostDefault();
        cut.FindAll("label").Any(l => l.TextContent.Contains("Container", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
