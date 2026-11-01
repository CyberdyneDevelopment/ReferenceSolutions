using Fdw.Services.Abstractions;

using Fdw.Services.Connections.Abstractions;
namespace FdwReferenceConnections.MsSql;

/// <summary>Supplies the MsSqlConnection implementation's own configuration.</summary>
public interface IMsSqlConnectionConfigurationProvider
    : IImplementationConfigurationProvider<IConnectionImplementationConfiguration>
{
}
