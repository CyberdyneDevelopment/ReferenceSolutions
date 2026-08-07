namespace Reference.Ui.Tests.Components;

public sealed class TransformStepCardTests : BunitContext
{
    private static FieldMappingTransformPayload MakeTransform(string type = "Format", int parameterCount = 0) => new()
    {
        Id = Guid.NewGuid(),
        FieldMappingId = Guid.NewGuid(),
        TransformType = type,
        Ordinal = 1,
        Parameters = Enumerable.Range(0, parameterCount)
            .Select(i => new FieldMappingTransformParameterPayload { Name = $"p{i}", Value = $"v{i}" })
            .ToList()
    };

    [Fact]
    public void RendersDisplayNameAndCategoryBadge()
    {
        var t = MakeTransform();
        var def = new TransformTypePayload { Name = "Format", DisplayName = "Format Date", Category = "DateTime" };

        var cut = Render<TransformStepCard>(p => p
            .Add(x => x.Transform, t)
            .Add(x => x.TypeDefinition, def)
            .Add(x => x.StepNumber, 3));

        cut.Markup.ShouldContain("Format Date");
        cut.Markup.ShouldContain("DateTime");
        cut.Markup.ShouldContain(">3<");
    }

    [Fact]
    public void RendersTransformTypeWhenDefinitionMissing()
    {
        var cut = Render<TransformStepCard>(p => p
            .Add(x => x.Transform, MakeTransform("RawTypeName"))
            .Add(x => x.StepNumber, 1));

        cut.Markup.ShouldContain("RawTypeName");
        cut.Markup.ShouldContain("No parameters configured");
    }

    [Fact]
    public void ShowsExpandedFallbackWhenNoTypeDefinition()
    {
        var cut = Render<TransformStepCard>(p => p
            .Add(x => x.Transform, MakeTransform())
            .Add(x => x.IsExpanded, true));

        cut.Markup.ShouldContain("Transform type definition not available");
    }

    [Fact]
    public void ShowsExpandedFormWhenTypeDefinitionPresent()
    {
        var def = new TransformTypePayload
        {
            Name = "Format",
            DisplayName = "Format",
            Parameters = new TransformParameterDefinitionPayload[] { new() { Name = "fmt" } }
        };

        var cut = Render<TransformStepCard>(p => p
            .Add(x => x.Transform, MakeTransform())
            .Add(x => x.TypeDefinition, def)
            .Add(x => x.IsExpanded, true));

        cut.FindComponent<TransformParameterForm>().ShouldNotBeNull();
    }

    [Fact]
    public void MoveButtonsDisabledOnEdges()
    {
        var cut = Render<TransformStepCard>(p => p
            .Add(x => x.Transform, MakeTransform())
            .Add(x => x.IsFirst, true)
            .Add(x => x.IsLast, true));

        var buttons = cut.FindAll("button[title]");
        buttons.First(b => b.GetAttribute("title") == "Move up").HasAttribute("disabled").ShouldBeTrue();
        buttons.First(b => b.GetAttribute("title") == "Move down").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void DeleteRequiresConfirmation()
    {
        var deleted = 0;
        var cut = Render<TransformStepCard>(p => p
            .Add(x => x.Transform, MakeTransform())
            .Add(x => x.OnDelete, EventCallback.Factory.Create(this, () => deleted++)));

        var delBtn = cut.FindAll("button[title]").First(b => b.GetAttribute("title") == "Delete transform");
        delBtn.Click();
        deleted.ShouldBe(0);

        delBtn.Click();
        deleted.ShouldBe(1);
    }

    [Fact]
    public void ParameterPreviewTruncatesAndCountsOverflow()
    {
        var t = MakeTransform(parameterCount: 5);
        var list = t.Parameters.ToList();
        list[1] = new FieldMappingTransformParameterPayload { Name = "p1", Value = new string('x', 50) };
        t.Parameters = list;

        var cut = Render<TransformStepCard>(p => p
            .Add(x => x.Transform, t));

        cut.Markup.ShouldContain("+2 more");
        cut.Markup.ShouldContain(new string('x', 20) + "...");
    }
}
