using Fdw.Data.Components.DataMapper;
using Reference.Ui.Tests.Infrastructure;
using MapperPage = Fdw.UI.Pages.Data.Pages.MapperPage;

namespace Reference.Ui.Tests.Components.Mapper;

/// <summary>
/// App-level host tests for the Field Mapper page as the reference-ui app routes to it. These
/// assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal picker / field-
/// list / mapping / auto-map / validate / save behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (DataMapper page-component tests).
/// </summary>
public sealed class MapperPageTests : BunitContext
{
    // Why: swap the live FDW DataMapperProvider for a stub seeded with a DEFAULT context so the page
    // renders deterministically without HTTP. The provider injects IHttpClientFactory, so the shared
    // provider infrastructure (a no-op IHttpClientFactory + ILoggerFactory) must be present.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new ProviderFactory<DataMapperProvider, DataMapperContext>(new DataMapperContext()));
    }

    [Fact]
    public void HostsMapperPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<MapperPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedMapperPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<MapperPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Field Mapper", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
