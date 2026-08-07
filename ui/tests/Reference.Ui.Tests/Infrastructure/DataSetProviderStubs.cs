using Fdw.Data.Components.DataSets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reference.Ui.Tests.Infrastructure;

/// <summary>
/// Concrete test double that <b>inherits <see cref="DataSetProvider"/></b> so a page's
/// <c>@ref</c> (typed to the real provider) casts successfully, while suppressing the
/// real provider's HTTP/lifecycle work and rendering the page against a seeded
/// <see cref="DataSetContext"/>.
/// </summary>
/// <remarks>
/// Pages such as <c>DataSetDetail</c> capture the provider via <c>@ref="_provider"</c>;
/// the unrelated <see cref="ProviderStub{TContext}"/> would throw
/// <see cref="InvalidCastException"/>. A concrete subclass is the only way the cast can
/// succeed because C# cannot make a generic class inherit a type parameter.
/// Lifecycle hooks are no-ops so the base provider's injected API client and HTTP loading
/// never run; <see cref="BuildRenderTree"/> renders the seeded context directly.
/// </remarks>
public sealed class StubDataSetProvider : DataSetProvider
{
    [CascadingParameter] private DataSetContextSeed? Seed { get; set; }

    protected override void OnInitialized() { /* Why: skip base HTTP client construction */ }

    protected override Task OnInitializedAsync() => Task.CompletedTask;

    protected override void OnParametersSet() { }

    protected override Task OnParametersSetAsync() => Task.CompletedTask;

    protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;

    protected override void OnAfterRender(bool firstRender) { }

    protected override bool ShouldRender() => true;

    protected override void BuildRenderTree(RenderTreeBuilder __builder)
    {
        if (ChildContent is not null)
        {
            __builder.AddContent(0, ChildContent(Seed?.Value ?? new DataSetContext()));
        }
    }
}

/// <summary>
/// Concrete test double inheriting <see cref="DataSetWizardProvider"/> for pages that
/// capture it via <c>@ref</c> (the DataSet wizard). Renders a seeded
/// <see cref="DataSetWizardContext"/> and skips all base lifecycle/HTTP work.
/// </summary>
public sealed class StubDataSetWizardProvider : DataSetWizardProvider
{
    [CascadingParameter] private DataSetWizardContextSeed? Seed { get; set; }

    protected override void OnInitialized() { }

    protected override Task OnInitializedAsync() => Task.CompletedTask;

    protected override void OnParametersSet() { }

    protected override Task OnParametersSetAsync() => Task.CompletedTask;

    protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;

    protected override void OnAfterRender(bool firstRender) { }

    protected override bool ShouldRender() => true;

    protected override void BuildRenderTree(RenderTreeBuilder __builder)
    {
        if (ChildContent is not null)
        {
            __builder.AddContent(0, ChildContent(Seed?.Value ?? new DataSetWizardContext()));
        }
    }
}

/// <summary>Cascading carrier for the seeded <see cref="DataSetContext"/>.</summary>
public sealed class DataSetContextSeed
{
    public DataSetContext Value { get; init; } = new();
}

/// <summary>Cascading carrier for the seeded <see cref="DataSetWizardContext"/>.</summary>
public sealed class DataSetWizardContextSeed
{
    public DataSetWizardContext Value { get; init; } = new();
}
