using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite;

/// <summary>
/// Factory interface for creating SQLite secret manager service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// Follows the ServiceType pattern where dependencies are injected via constructor
/// and configuration is passed to the Get method at runtime.
/// </remarks>
public interface ISqliteSecretManagerFactory : ISecretManagerServiceFactory<ISecretManager, SqliteSecretManagerConfiguration>
{
    // Inherits CreateSecretManager(SqliteSecretManagerConfiguration) from base interface
}
