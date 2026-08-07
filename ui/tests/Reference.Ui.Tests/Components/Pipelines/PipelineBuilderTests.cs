using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.UI.Pipelines.Clients;
using Fdw.UI.Pipelines.Clients.Models;
using Moq;
using Reference.Ui.Tests.Infrastructure;
using BuilderPage = Fdw.UI.Pages.Pipelines.Pages.Pipelines.PipelineBuilderPage;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// App-level host tests for the Pipeline Builder page as the reference-ui app routes to it. These
/// assert ONLY that the app hosts the FDW page and that its toolbar landmark renders with a DEFAULT
/// (unseeded) provider state — they do NOT assert the FDW component's internal canvas / palette /
/// toolbar-state / hydration / test-run behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (PipelineBuilder page-component tests).
/// </summary>
public sealed class PipelineBuilderTests : BunitContext
{
    // Why: the page captures its PipelineBuilderProvider via a typed @ref, so the provider cannot be
    // swapped for a stub. Render the REAL provider with a Moq IPipelineDesignerClient returning a
    // DEFAULT (empty) task-type list, plus the shared provider infrastructure (a no-op
    // IHttpClientFactory + ILoggerFactory + IAccessTokenProvider) the page and provider inject. Since
    // the FDW-572 canvas reprojection the provider's load/save flow is IPipelineDesignerClient-only —
    // no gateway-backed configuration provider exists on the UI path, so no ConfigurationDb wiring
    // (real or mocked) is involved at all.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        var designer = new Mock<IPipelineDesignerClient>(MockBehavior.Loose);
        designer.Setup(d => d.GetTaskTypes(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TaskTypeInfo>>.Success([]));
        Services.AddSingleton(designer.Object);
        // Why: PipelineBuilderProvider also property-injects IPipelineClient (the pipeline
        // configuration client, distinct from the designer client). Blazor property injection is
        // required-by-default, so an unregistered one fails the render outright rather than leaving
        // the property null. Loose mock: these tests assert the DEFAULT unseeded render only.
        Services.AddSingleton(new Mock<IPipelineClient>(MockBehavior.Loose).Object);
    }

    [Fact]
    public void HostsPipelineBuilderPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<BuilderPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedPipelineBuilderPageRendersToolbarLandmark()
    {
        HostDefault();
        var cut = Render<BuilderPage>();
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".content.builder").Count.ShouldBeGreaterThan(0);
            cut.FindAll("h1").Any(h => h.TextContent.Contains("Pipeline Builder", StringComparison.Ordinal)).ShouldBeTrue();
        });
    }
}
