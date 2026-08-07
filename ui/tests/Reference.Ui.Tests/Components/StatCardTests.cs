namespace Reference.Ui.Tests.Components;

public sealed class StatCardTests : BunitContext
{
    [Fact]
    public void RendersTitleAndValue()
    {
        var cut = Render<StatCard>(p => p
            .Add(x => x.Title, "Active Pipelines")
            .Add(x => x.Value, "42")
            .Add(x => x.Color, "green"));

        // Why: the metric label moved from an <h3> to a `.lbl` span and the value from
        // a Tailwind `.text-2xl` node to a `.val` span when the card was reskinned.
        cut.Find(".lbl").TextContent.Trim().ShouldBe("Active Pipelines");
        cut.Find(".val").TextContent.Trim().ShouldBe("42");
    }

    [Theory]
    [InlineData("green", "var(--success)")]
    [InlineData("cyan", "var(--glacier)")]
    [InlineData("yellow", "var(--warn)")]
    [InlineData("red", "var(--signal)")]
    [InlineData("purple", "var(--amber)")]
    public void AppliesColorToIcon(string color, string expectedColorVar)
    {
        var cut = Render<StatCard>(p => p
            .Add(x => x.Title, "T")
            .Add(x => x.Value, "1")
            .Add(x => x.Color, color));

        // Why: colour is now expressed as an inline `color:` CSS var on the `.ic` icon
        // wrapper (GetIconColor), not a Tailwind text-* class on the value.
        cut.Find(".ic").GetAttribute("style").ShouldNotBeNull()
            .ShouldContain("color:" + expectedColorVar, Case.Sensitive);
    }

    [Fact]
    public void DefaultsRenderWithoutCrashing()
    {
        var cut = Render<StatCard>();
        cut.Find(".val").TextContent.Trim().ShouldBe("0");
    }
}
