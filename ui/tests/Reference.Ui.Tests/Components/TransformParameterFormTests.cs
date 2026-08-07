namespace Reference.Ui.Tests.Components;

public sealed class TransformParameterFormTests : BunitContext
{
    [Fact]
    public void NoParametersShowsEmptyMessage()
    {
        var cut = Render<TransformParameterForm>(p => p
            .Add(x => x.ParameterDefinitions, Array.Empty<TransformParameterDefinitionPayload>()));

        cut.Markup.ShouldContain("This transform has no configurable parameters.");
    }

    [Fact]
    public void RendersRequiredMarkerDisplayNameAndKind()
    {
        var defs = new TransformParameterDefinitionPayload[]
        {
            new() { Name = "fmt", DisplayName = "Format", IsRequired = true, Kind = "Scalar" },
            new() { Name = "src", DisplayName = "", IsRequired = false, Kind = "Field", HelpText = "source field" }
        };

        var cut = Render<TransformParameterForm>(p => p
            .Add(x => x.ParameterDefinitions, defs));

        cut.Markup.ShouldContain("Format");
        cut.Markup.ShouldContain("*");
        cut.Markup.ShouldContain("(Field)");
        cut.Markup.ShouldNotContain("(Scalar)");
        cut.Markup.ShouldContain("source field");
    }

    [Fact]
    public void CurrentParametersPopulateInputValues()
    {
        var defs = new TransformParameterDefinitionPayload[] { new() { Name = "fmt" } };
        var current = new FieldMappingTransformParameterPayload[] { new() { Name = "fmt", Value = "yyyy-MM-dd" } };

        var cut = Render<TransformParameterForm>(p => p
            .Add(x => x.ParameterDefinitions, defs)
            .Add(x => x.CurrentParameters, current));

        cut.Find("input").GetAttribute("value").ShouldBe("yyyy-MM-dd");
    }

    [Fact]
    public void SaveButtonDisabledUntilUserEdits()
    {
        var defs = new TransformParameterDefinitionPayload[] { new() { Name = "fmt" } };
        var cut = Render<TransformParameterForm>(p => p
            .Add(x => x.ParameterDefinitions, defs));

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();

        cut.Find("input").Change("abc");
        cut.Find("button").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void SaveShowsValidationErrorWhenRequiredMissing()
    {
        IList<SaveTransformParameterRequest>? saved = null;
        var defs = new TransformParameterDefinitionPayload[]
        {
            new() { Name = "fmt", DisplayName = "Format", IsRequired = true }
        };

        var cut = Render<TransformParameterForm>(p => p
            .Add(x => x.ParameterDefinitions, defs)
            .Add(x => x.OnSave, EventCallback.Factory.Create<IList<SaveTransformParameterRequest>>(this, v => saved = v)));

        cut.Find("input").Change("   ");
        cut.Find("button").Click();

        saved.ShouldBeNull();
        cut.Markup.ShouldContain("Format is required.");
    }

    [Fact]
    public void SaveEmitsNonEmptyParametersOnSuccess()
    {
        IList<SaveTransformParameterRequest>? saved = null;
        var defs = new TransformParameterDefinitionPayload[]
        {
            new() { Name = "fmt", IsRequired = true },
            new() { Name = "tz", IsRequired = false }
        };

        var cut = Render<TransformParameterForm>(p => p
            .Add(x => x.ParameterDefinitions, defs)
            .Add(x => x.OnSave, EventCallback.Factory.Create<IList<SaveTransformParameterRequest>>(this, v => saved = v)));

        cut.FindAll("input")[0].Change("yyyy");
        cut.FindAll("input")[1].Change("");

        cut.Find("button").Click();

        saved.ShouldNotBeNull();
        saved.Count.ShouldBe(1);
        saved[0].Name.ShouldBe("fmt");
        saved[0].Value.ShouldBe("yyyy");
    }

    [Fact]
    public void IsSavingDisablesSaveButtonAndShowsSpinner()
    {
        var defs = new TransformParameterDefinitionPayload[] { new() { Name = "fmt" } };
        var cut = Render<TransformParameterForm>(p => p
            .Add(x => x.ParameterDefinitions, defs)
            .Add(x => x.IsSaving, true));

        cut.Find("input").Change("v");
        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("button .animate-spin").ShouldNotBeNull();
    }
}
