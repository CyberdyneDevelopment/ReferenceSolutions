using Bunit.ComponentFactories;
using Fdw.Services.Messaging.Components.Messaging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reference.Ui.Tests.Infrastructure;

/// <summary>
/// Stand-in for the multi-fragment <c>MessageProvider</c>, which exposes three named
/// RenderFragments (MessageListContent / MessageDetailContent / AccessRequestContent) rather
/// than a single ChildContent. Renders whichever fragment the consuming page supplies, passing
/// the seeded context. Seeds are taken from <see cref="ProviderStubState"/> keyed by context type.
/// Extra params (AutoLoad, AutoLoadMessageId, AutoLoadAccessRequests, …) are absorbed.
/// </summary>
public sealed class MessageProviderStub : ComponentBase
{
    [Parameter] public RenderFragment<MessageListContext>? MessageListContent { get; set; }
    [Parameter] public RenderFragment<MessageDetailContext>? MessageDetailContent { get; set; }
    [Parameter] public RenderFragment<AccessRequestListContext>? AccessRequestContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? Extra { get; set; }

    private MessageListContext _list = new();
    private MessageDetailContext _detail = new();
    private AccessRequestListContext _access = new();

    protected override void OnInitialized()
    {
        // Why: Take<T> removes the seed from the shared store, so it must be captured ONCE here.
        // Re-reading it on every BuildRenderTree (e.g. after a filter @onclick) would yield an
        // empty context on the second render and drop the seeded rows.
        _list = ProviderStubState.Take<MessageListContext>() ?? new MessageListContext();
        _detail = ProviderStubState.Take<MessageDetailContext>() ?? new MessageDetailContext();
        _access = ProviderStubState.Take<AccessRequestListContext>() ?? new AccessRequestListContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (MessageListContent is not null)
        {
            builder.AddContent(0, MessageListContent(_list));
        }
        if (MessageDetailContent is not null)
        {
            builder.AddContent(1, MessageDetailContent(_detail));
        }
        if (AccessRequestContent is not null)
        {
            builder.AddContent(2, AccessRequestContent(_access));
        }
    }
}

/// <summary>
/// bUnit IComponentFactory swapping the real <c>MessageProvider</c> for
/// <see cref="MessageProviderStub"/>. Seeds the relevant context(s) before render.
/// </summary>
public sealed class MessageProviderFactory : IComponentFactory
{
    private readonly Action _seed;

    public MessageProviderFactory(MessageListContext? list = null, MessageDetailContext? detail = null, AccessRequestListContext? access = null)
        => _seed = () =>
        {
            if (list is not null) ProviderStubState.Set(list);
            if (detail is not null) ProviderStubState.Set(detail);
            if (access is not null) ProviderStubState.Set(access);
        };

    public bool CanCreate(Type componentType) => componentType == typeof(MessageProvider);

    public IComponent Create(Type componentType)
    {
        _seed();
        return new MessageProviderStub();
    }
}
