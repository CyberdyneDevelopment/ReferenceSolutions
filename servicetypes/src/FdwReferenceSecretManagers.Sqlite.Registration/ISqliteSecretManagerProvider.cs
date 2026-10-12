using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceSecretManagers.Sqlite.Registration;

/// <summary>The SqliteSecretManager implementation of its domain.</summary>
public interface ISqliteSecretManagerProvider : IImplementationServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>
{
}
