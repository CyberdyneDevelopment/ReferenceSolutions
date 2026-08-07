using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets;

/// <summary>
/// Factory interface for creating User Secrets secret manager service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// Follows the ServiceType pattern where dependencies are injected via constructor
/// and configuration is passed to the Get method at runtime.
/// </remarks>
public interface IUserSecretsSecretManagerFactory : ISecretManagerServiceFactory<ISecretManager, UserSecretsConfiguration>
{
    // Inherits Get(UserSecretsConfiguration) from base interface
}
