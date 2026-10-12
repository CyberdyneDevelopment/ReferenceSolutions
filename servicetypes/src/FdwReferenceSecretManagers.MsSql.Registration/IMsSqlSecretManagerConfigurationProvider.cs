using Fdw.Services.Abstractions;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.MsSql.Registration;

/// <summary>Supplies the MsSqlSecretManager implementation's own configuration.</summary>
public interface IMsSqlSecretManagerConfigurationProvider
    : IImplementationConfigurationProvider<ISecretManagerImplementationConfiguration>
{
}
