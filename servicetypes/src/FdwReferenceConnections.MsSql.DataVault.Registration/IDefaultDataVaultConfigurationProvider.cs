using Fdw.Services.Abstractions;

using Fdw.Services.DataVault.Abstractions;
namespace FdwReferenceConnections.MsSql.DataVault.Registration;

/// <summary>Supplies the DefaultDataVault implementation's own configuration.</summary>
public interface IDefaultDataVaultConfigurationProvider
    : IImplementationConfigurationProvider<IDataVaultImplementationConfiguration>
{
}
