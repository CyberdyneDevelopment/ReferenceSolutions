using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Catalog.Components.Catalog;
using Fdw.Services.Catalog.Clients.Models;
using Reference.Ui.Tests.Infrastructure;
using CatalogPage = Fdw.UI.Pages.Catalog.Pages.CatalogPage;

namespace Reference.Ui.Tests.Components.Data;

/// <summary>
/// bUnit tests for the FDW <c>Catalog</c> page (<c>Fdw.UI.Pages.Catalog.Pages.CatalogPage</c>), hosting a
/// stubbed <see cref="CatalogProvider"/> (swapped via <see cref="ProviderFactory{TActual,TContext}"/>)
/// seeded with a <see cref="CatalogContext"/> so markup renders deterministically without HTTP.
/// </summary>
public sealed class CatalogPageTests : BunitContext
{
    private void Swap(CatalogContext? seed = null) =>
        ComponentFactories.Add(new ProviderFactory<CatalogProvider, CatalogContext>(seed));

    private static void ClickButton(IRenderedComponent<CatalogPage> cut, string label) =>
        cut.FindAll("button").First(b => b.TextContent.Trim() == label).Click();

    [Fact]
    public void DefaultRendersHeadingAndEmptyState()
    {
        Swap();
        var cut = Render<CatalogPage>();
        cut.Markup.ShouldContain("Catalog");
        cut.Markup.ShouldContain("No catalog entries.");
    }

    [Fact]
    public void RendersErrorBanner()
    {
        Swap(new CatalogContext { LastResult = GenericResult.Failure(new GenericMessage { Message = "oops" }) });
        var cut = Render<CatalogPage>();
        cut.Markup.ShouldContain("oops");
    }

    [Fact]
    public void RendersLoadingIndicator()
    {
        Swap(new CatalogContext { IsLoading = true });
        var cut = Render<CatalogPage>();
        cut.Markup.ShouldContain("Loading catalog…");
    }

    [Fact]
    public void RendersDataSetRowWithActions()
    {
        Swap(new CatalogContext
        {
            DataSets = [new CatalogEntityPayload { Name = "Teams", EntityType = "DataSet" }]
        });
        var cut = Render<CatalogPage>();
        cut.Markup.ShouldContain("Teams");
        cut.Markup.ShouldContain("DataSet");
        cut.Markup.ShouldContain("Open");
        cut.Markup.ShouldContain("Explore Lineage");
        cut.Markup.ShouldContain("Start Builder");
        cut.Markup.ShouldContain("Derive DataSet");
    }

    [Fact]
    public void RendersNonDataSetRowWithoutActions()
    {
        Swap(new CatalogContext
        {
            DataSets = [new CatalogEntityPayload { Name = "TeamsTable", EntityType = "Container" }]
        });
        var cut = Render<CatalogPage>();
        cut.Markup.ShouldContain("TeamsTable");
        cut.Markup.ShouldContain("Container");
        cut.Markup.ShouldNotContain("Start Builder");
        cut.Markup.ShouldNotContain("Derive DataSet");
    }

    [Fact]
    public async Task ClickingSearchInvokesOnSearchWithTypedQuery()
    {
        var queries = new List<string>();
        Swap(new CatalogContext { OnSearch = q => { queries.Add(q); return Task.CompletedTask; } });
        var cut = Render<CatalogPage>();

        cut.Find("input.fin").Input("teams");
        ClickButton(cut, "Search");
        await Task.Yield();

        queries.ShouldContain("teams");
    }

    [Fact]
    public async Task ClickingRefreshInvokesOnRefresh()
    {
        var refreshed = false;
        Swap(new CatalogContext { OnRefresh = () => { refreshed = true; return Task.FromResult(GenericResult.Success()); } });
        var cut = Render<CatalogPage>();

        ClickButton(cut, "Refresh");
        await Task.Yield();

        refreshed.ShouldBeTrue();
    }
}
