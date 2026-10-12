using RootNamespace.ServiceName.Abstractions;

namespace RootNamespace.ServiceName.ImplName;

/// <summary>
/// Builds ImplName ServiceName services from their configuration.
/// </summary>
/// <remarks>
/// Named, so the option registers against it and the implementation provider depends on it, rather
/// than either spelling out the domain's generic factory closure.
/// </remarks>
public interface IImplNameServiceNameFactory
    : IServiceNameFactory<IServiceNameService, ImplNameServiceNameConfiguration>
{
}
