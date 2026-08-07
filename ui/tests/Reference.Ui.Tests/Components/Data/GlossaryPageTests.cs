using Fdw.Services.Catalog.Components.Glossary;
using Reference.Ui.Tests.Infrastructure;
using GlossaryPage = Fdw.UI.Pages.Catalog.Pages.Glossary.GlossaryIndexPage;

namespace Reference.Ui.Tests.Components.Data;

/// <summary>
/// App-level host tests for the Glossary page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// data/filter/loading behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Glossary component tests).
/// </summary>
public sealed class GlossaryPageTests : BunitContext
{
    // Why: swap the live FDW GlossaryProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<GlossaryProvider, GlossaryContext>(new GlossaryContext()));

    [Fact]
    public void HostsGlossaryPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<GlossaryPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedGlossaryPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<GlossaryPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Glossary", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
