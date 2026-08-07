using System.Collections.Generic;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Moq;
using PipelineIndexPage = Fdw.UI.Pages.Pipelines.Pages.Pipelines.PipelinesIndexPage;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// App-level host tests for the Pipelines registry page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider state — they do NOT assert the FDW component's internal empty / populated /
/// failure / trigger behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Pipelines Index page-component tests).
/// </summary>
public sealed class PipelineIndexTests : BunitContext
{
    // Why: the page captures its PipelineProvider via a typed @ref, so the provider cannot be
    // swapped for a stub. Render the REAL provider with Moq clients returning a DEFAULT (empty)
    // success list so the page settles deterministically without HTTP — the app only owns
    // routing/hosting here.
    private void HostDefault()
    {
        var client = new Mock<IPipelineClient>(MockBehavior.Loose);
        client.Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Success([]));
        Services.AddSingleton(client.Object);
        Services.AddSingleton(new Mock<IPipelineJobClient>(MockBehavior.Loose).Object);
    }

    [Fact]
    public void HostsPipelineIndexPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<PipelineIndexPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedPipelineIndexPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<PipelineIndexPage>();
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
            cut.FindAll("h1").Any(h => h.TextContent.Contains("Pipelines", StringComparison.Ordinal)).ShouldBeTrue();
        });
    }
}
