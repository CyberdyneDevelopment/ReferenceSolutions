using Fdw.Services.Abstractions;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.Sqlite.Registration;

/// <summary>Supplies the SqliteSecretManager implementation's own configuration.</summary>
public interface ISqliteSecretManagerConfigurationProvider
    : IImplementationConfigurationProvider<ISecretManagerImplementationConfiguration>
{
}
