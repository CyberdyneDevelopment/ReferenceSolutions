using Fdw.Results;
using Fdw.Messages;
using Fdw.Data.Components.DataSets;
using Reference.Management.UI.Tailwind.Components.Domain.DataSets;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components;

public sealed class TransformChainEditorTests : BunitContext
{
    private void Swap(FieldMappingTransformContext? seed = null) =>
        ComponentFactories.Add(new ProviderFactory<FieldMappingTransformProvider, FieldMappingTransformContext>(seed));

    private static FieldMappingTransformPayload Step(int ordinal, string type = "Format") => new()
    {
        Id = Guid.NewGuid(),
        FieldMappingId = Guid.NewGuid(),
        TransformType = type,
        Ordinal = ordinal,
        Parameters = Array.Empty<FieldMappingTransformParameterPayload>()
    };

    [Fact]
    public void RendersHeaderAndAddButton()
    {
        Swap(new FieldMappingTransformContext { FieldMappingName = "userId" });
        var cut = Render<TransformChainEditor>(p => p.Add(x => x.FieldMappingId, Guid.NewGuid()));
        cut.Markup.ShouldContain("Transform Chain");
        cut.Markup.ShouldContain("userId");
        cut.FindAll("button").Any(b => b.TextContent.Contains("Add Transform")).ShouldBeTrue();
    }

    [Fact]
    public void RendersErrorBannerWhenContextHasError()
    {
        Swap(new FieldMappingTransformContext { LastResult = GenericResult.Failure(new GenericMessage { Message = "boom" }) });
        var cut = Render<TransformChainEditor>(p => p.Add(x => x.FieldMappingId, Guid.NewGuid()));
        cut.Markup.ShouldContain("boom");
    }

    [Fact]
    public void RendersLoadingSpinnerWhenLoading()
    {
        Swap(new FieldMappingTransformContext { IsLoading = true });
        var cut = Render<TransformChainEditor>(p => p.Add(x => x.FieldMappingId, Guid.NewGuid()));
        cut.Find(".animate-spin").ShouldNotBeNull();
    }

    [Fact]
    public void RendersEmptyStateWhenNoTransforms()
    {
        Swap(new FieldMappingTransformContext());
        var cut = Render<TransformChainEditor>(p => p.Add(x => x.FieldMappingId, Guid.NewGuid()));
        cut.Markup.ShouldContain("No transforms configured.");
    }

    [Fact]
    public void RendersStepCardsOrderedByOrdinal()
    {
        Swap(new FieldMappingTransformContext
        {
            Transforms = new[] { Step(3, "ToUpper"), Step(1, "Trim"), Step(2, "FormatDate") }
        });
        var cut = Render<TransformChainEditor>(p => p.Add(x => x.FieldMappingId, Guid.NewGuid()));
        var cards = cut.FindComponents<TransformStepCard>();
        cards.Count.ShouldBe(3);
        cards[0].Instance.Transform.TransformType.ShouldBe("Trim");
        cards[1].Instance.Transform.TransformType.ShouldBe("FormatDate");
        cards[2].Instance.Transform.TransformType.ShouldBe("ToUpper");
    }

    [Fact]
    public async Task AddTransformButtonOpensTypeSelectorAndSelectingFiresAddTransform()
    {
        string? added = null;
        Swap(new FieldMappingTransformContext
        {
            AvailableTransformTypes = new[]
            {
                new TransformTypePayload { Name = "Trim", DisplayName = "Trim", Category = "String" }
            },
            OnAddTransform = name => { added = name; return Task.CompletedTask; }
        });
        var cut = Render<TransformChainEditor>(p => p.Add(x => x.FieldMappingId, Guid.NewGuid()));

        cut.FindAll("button").First(b => b.TextContent.Contains("Add Transform")).Click();
        // selector now mounted
        cut.FindComponent<TransformTypeSelector>().ShouldNotBeNull();

        // click the "Trim" type option
        cut.FindAll("button.w-full").First(b => b.TextContent.Contains("Trim")).Click();
        await Task.Yield();

        added.ShouldBe("Trim");
    }

    [Fact]
    public void StepCardToggleExpandOpensExactlyOneCardAtATime()
    {
        var s1 = Step(1, "Trim");
        var s2 = Step(2, "FormatDate");
        Swap(new FieldMappingTransformContext
        {
            Transforms = new[] { s1, s2 },
            AvailableTransformTypes = new[]
            {
                new TransformTypePayload { Name = "Trim", DisplayName = "Trim" },
                new TransformTypePayload { Name = "FormatDate", DisplayName = "Format Date" }
            }
        });
        var cut = Render<TransformChainEditor>(p => p.Add(x => x.FieldMappingId, Guid.NewGuid()));

        var cards = cut.FindComponents<TransformStepCard>();
        cards.Count(c => c.Instance.IsExpanded).ShouldBe(0);

        // Expand the first
        cut.FindAll("div.cursor-pointer")[0].Click();
        cards = cut.FindComponents<TransformStepCard>();
        cards.Count(c => c.Instance.IsExpanded).ShouldBe(1);
        cards[0].Instance.IsExpanded.ShouldBeTrue();

        // Expand the second — first should collapse
        cut.FindAll("div.cursor-pointer")[1].Click();
        cards = cut.FindComponents<TransformStepCard>();
        cards.Count(c => c.Instance.IsExpanded).ShouldBe(1);
        cards[1].Instance.IsExpanded.ShouldBeTrue();
    }
}
