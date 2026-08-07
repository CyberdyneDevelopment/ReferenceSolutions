using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.EnvironmentVariable;

/// <summary>
/// Factory interface for creating Environment Variable secret manager service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// Follows the ServiceType pattern where dependencies are injected via constructor
/// and configuration is passed to the Get method at runtime.
/// </remarks>
public interface IEnvironmentVariableSecretManagerFactory : ISecretManagerServiceFactory<ISecretManager, EnvironmentVariableConfiguration>
{
    // Inherits Get(EnvironmentVariableConfiguration) from base interface
}
