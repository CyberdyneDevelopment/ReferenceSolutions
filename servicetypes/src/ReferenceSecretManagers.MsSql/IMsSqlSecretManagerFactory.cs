using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql;

/// <summary>
/// Factory interface for creating MsSql secret manager service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// Follows the ServiceType pattern where dependencies are injected via constructor
/// and configuration is passed to the Get method at runtime.
/// </remarks>
public interface IMsSqlSecretManagerFactory : ISecretManagerServiceFactory<ISecretManager, MsSqlSecretManagerConfiguration>
{
    // Inherits Get(MsSqlSecretManagerConfiguration) from base interface
}
