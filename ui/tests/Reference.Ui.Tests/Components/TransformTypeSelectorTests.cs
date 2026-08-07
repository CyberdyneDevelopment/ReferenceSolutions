namespace Reference.Ui.Tests.Components;

public sealed class TransformTypeSelectorTests : BunitContext
{
    private static TransformTypePayload[] Types() => new TransformTypePayload[]
    {
        new() { Name = "ToUpper", DisplayName = "To Upper", Category = "String", Description = "Uppercase" },
        new() { Name = "Trim",    DisplayName = "Trim",     Category = "String", SupportsBatching = true },
        new() { Name = "Round",   DisplayName = "Round",    Category = "Numeric",
                Parameters = new TransformParameterDefinitionPayload[] { new() { Name = "digits" } } },
        new() { Name = "FormatDate", DisplayName = "Format Date", Category = "DateTime" },
        new() { Name = "When", DisplayName = "When", Category = "Conditional" },
        new() { Name = "Static", DisplayName = "Static", Category = "" }
    };

    [Fact]
    public void GroupsByCategoryInDefinedOrder()
    {
        var cut = Render<TransformTypeSelector>(p => p
            .Add(x => x.AvailableTypes, Types()));

        var headers = cut.FindAll(".text-\\[10px\\].uppercase")
            .Select(e => e.TextContent.Trim())
            .ToList();

        headers.IndexOf("String").ShouldBeLessThan(headers.IndexOf("Numeric"));
        headers.IndexOf("Numeric").ShouldBeLessThan(headers.IndexOf("DateTime"));
        headers.IndexOf("DateTime").ShouldBeLessThan(headers.IndexOf("Conditional"));
        headers.ShouldContain("Other");
    }

    [Fact]
    public void SearchFiltersByNameDisplayNameDescriptionAndCategory()
    {
        var cut = Render<TransformTypeSelector>(p => p
            .Add(x => x.AvailableTypes, Types()));

        // Why: "Round" appears in tailwind class names like "rounded"; assert against the
        // option-button text instead of raw markup.
        cut.Find("input").Input("uppercase");
        ButtonTexts(cut).ShouldContain(s => s.Contains("To Upper", StringComparison.OrdinalIgnoreCase));
        ButtonTexts(cut).ShouldNotContain(s => s.Contains("Round", StringComparison.OrdinalIgnoreCase));

        cut.Find("input").Input("numeric");
        ButtonTexts(cut).ShouldContain(s => s.Contains("Round", StringComparison.OrdinalIgnoreCase));
        ButtonTexts(cut).ShouldNotContain(s => s.Contains("To Upper", StringComparison.OrdinalIgnoreCase));

        static List<string> ButtonTexts(IRenderedComponent<TransformTypeSelector> c) =>
            c.FindAll("button[class*='w-full']").Select(b => b.TextContent).ToList();
    }

    [Fact]
    public void EmptySearchShowsAllAndNotEmptyMessage()
    {
        var cut = Render<TransformTypeSelector>(p => p
            .Add(x => x.AvailableTypes, Types()));

        cut.Markup.ShouldNotContain("No transforms match");
        cut.FindAll("button[class*='w-full']").Count.ShouldBeGreaterThanOrEqualTo(6);
    }

    [Fact]
    public void NoMatchesShowsEmptyMessage()
    {
        var cut = Render<TransformTypeSelector>(p => p
            .Add(x => x.AvailableTypes, Types()));

        cut.Find("input").Input("zzzzz-nope");
        cut.Markup.ShouldContain("No transforms match your search.");
    }

    [Fact]
    public void SelectingInvokesOnSelectWithTypeName()
    {
        string? picked = null;
        var cut = Render<TransformTypeSelector>(p => p
            .Add(x => x.AvailableTypes, Types())
            .Add(x => x.OnSelect, EventCallback.Factory.Create<string>(this, n => picked = n)));

        var trimBtn = cut.FindAll("button[class*='w-full']").First(b => b.TextContent.Contains("Trim"));
        trimBtn.Click();

        picked.ShouldBe("Trim");
    }

    [Fact]
    public void CloseButtonInvokesOnClose()
    {
        var closed = 0;
        var cut = Render<TransformTypeSelector>(p => p
            .Add(x => x.AvailableTypes, Types())
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closed++)));

        var headerCloseBtn = cut.FindAll("button").First(b => !b.ClassList.Contains("w-full"));
        headerCloseBtn.Click();
        closed.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void ShowsParamCountAndBatchBadge()
    {
        var cut = Render<TransformTypeSelector>(p => p
            .Add(x => x.AvailableTypes, Types()));

        cut.Markup.ShouldContain("1 param");
        cut.Markup.ShouldContain("batch");
    }
}
