using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reference.Ui.Tests.Infrastructure;

/// <summary>
/// Non-generic static store for pending context objects keyed by context type.
/// Separating static state from the generic class avoids CA1000.
/// </summary>
internal static class ProviderStubState
{
    private static readonly Dictionary<Type, object?> s_pending = [];

    internal static void Set<TContext>(TContext? value) =>
        s_pending[typeof(TContext)] = value;

    internal static TContext? Take<TContext>() where TContext : new()
    {
        if (s_pending.TryGetValue(typeof(TContext), out var value))
        {
            s_pending.Remove(typeof(TContext));
            return (TContext?)value;
        }
        return default;
    }
}

/// <summary>
/// Stand-in for any FDW headless provider whose child template is
/// <c>RenderFragment&lt;TContext&gt;</c>. Lets a test render the consuming
/// page's UI without standing up the provider's HTTP/service stack.
/// Captures any non-ChildContent parameters so providers with extra args
/// (RefreshInterval, DefaultDays, etc.) don't fail to bind.
/// </summary>
public sealed class ProviderStub<TContext> : ComponentBase
    where TContext : new()
{
    [Parameter] public RenderFragment<TContext>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? Extra { get; set; }

    private TContext _context = new();

    protected override void OnInitialized()
    {
        // Why: PendingContext was previously a static property on this generic type, which CA1000
        // flags. State is now stored in the non-generic ProviderStubState keyed by TContext type.
        _context = ProviderStubState.Take<TContext>() ?? new TContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            builder.AddContent(0, ChildContent(_context));
        }
    }
}
