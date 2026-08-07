using Bunit.ComponentFactories;
using Microsoft.AspNetCore.Components;

namespace Reference.Ui.Tests.Infrastructure;

/// <summary>
/// bUnit component factory that swaps a real FDW provider <typeparamref name="TActual"/>
/// for a concrete subclass <typeparamref name="TStub"/> that inherits it. Because the
/// stub IS-A <typeparamref name="TActual"/>, a consuming page's <c>@ref</c> (typed to
/// the real provider) casts successfully.
/// </summary>
public sealed class InheritingProviderFactory<TActual, TStub> : IComponentFactory
    where TActual : IComponent
    where TStub : TActual, new()
{
    public bool CanCreate(Type componentType) => componentType == typeof(TActual);

    public IComponent Create(Type componentType) => new TStub();
}
