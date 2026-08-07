using Bunit.ComponentFactories;
using Microsoft.AspNetCore.Components;

namespace Reference.Ui.Tests.Infrastructure;

/// <summary>
/// bUnit IComponentFactory that swaps a real FDW provider for a
/// <see cref="ProviderStub{TContext}"/>. Use one per provider type.
/// </summary>
public sealed class ProviderFactory<TActual, TContext> : IComponentFactory
    where TActual : IComponent
    where TContext : new()
{
    private readonly TContext? _seed;

    public ProviderFactory(TContext? seed = default) => _seed = seed;

    public bool CanCreate(Type componentType) => componentType == typeof(TActual);

    public IComponent Create(Type componentType)
    {
        if (_seed is not null)
        {
            ProviderStubState.Set(_seed);
        }
        return new ProviderStub<TContext>();
    }
}
