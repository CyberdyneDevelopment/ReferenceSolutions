using System.Net.Http;
using Fdw.Data.Components.DataSets;
using Fdw.Services.Data.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Data;

/// <summary>
/// App-level host tests for the Calculation Designer page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// designer/canvas/save behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (CalculatedDesigner component tests).
/// </summary>
public sealed class CalculatedDesignerPageTests : BunitContext
{
    // Why: swap the live FDW CalculatedDataSetProvider for a stub seeded with a DEFAULT context,
    // register provider infrastructure (IHttpClientFactory/ILoggerFactory), and register the
    // page's [Inject] DataStoreApiClient (no request is sent — the page only renders) — the app
    // only owns routing/hosting here.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        Services.AddSingleton(new DataStoreApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }, NullLogger<DataStoreApiClient>.Instance));
        ComponentFactories.Add(new ProviderFactory<CalculatedDataSetProvider, CalculatedDataSetContext>(new CalculatedDataSetContext()));
    }

    [Fact]
    public void HostsCalculatedDesignerPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Calculations.Pages.CalculatedDesignerPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedCalculatedDesignerPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Calculations.Pages.CalculatedDesignerPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Calculation Designer", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
