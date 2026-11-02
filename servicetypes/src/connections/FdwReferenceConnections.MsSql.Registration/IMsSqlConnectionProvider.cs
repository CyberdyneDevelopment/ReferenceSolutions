using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// The MsSql implementation of the Connection domain: resolves what an MsSql connection needs
/// fetched, then builds it.
/// </summary>
/// <remarks>
/// The domain provider holds the registry and dispatches here on the domain row's
/// <c>Implementation</c>; this holds no registry and knows one implementation. Registered into the
/// domain provider exactly as an implementation configuration provider is.
/// </remarks>
public interface IMsSqlConnectionProvider
    : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
{
}
