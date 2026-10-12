using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.EnvironmentVariable.Configuration;

namespace FdwReferenceSecretManagers.EnvironmentVariable;

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
