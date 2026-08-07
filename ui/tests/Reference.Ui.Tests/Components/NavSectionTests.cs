namespace Reference.Ui.Tests.Components;

/// <summary>
/// bUnit coverage for the <c>NavSection</c> layout component. The component was simplified to a
/// static section header — it renders its <c>Title</c> in the <c>.st</c> header and always renders
/// its <c>ChildContent</c>. (The former collapse/toggle/key behaviour was removed from this
/// component.)
/// </summary>
public sealed class NavSectionTests : BunitContext
{
    [Fact]
    public void RendersTitleInHeader()
    {
        var cut = Render<NavSection>(p => p
            .Add(x => x.Title, "Data")
            .AddChildContent("<span id='child'>X</span>"));

        cut.Find(".st").TextContent.ShouldBe("Data");
    }

    [Fact]
    public void RendersChildContent()
    {
        var cut = Render<NavSection>(p => p
            .Add(x => x.Title, "Data")
            .AddChildContent("<span id='child'>X</span>"));

        cut.Find("#child").TextContent.ShouldBe("X");
    }
}
