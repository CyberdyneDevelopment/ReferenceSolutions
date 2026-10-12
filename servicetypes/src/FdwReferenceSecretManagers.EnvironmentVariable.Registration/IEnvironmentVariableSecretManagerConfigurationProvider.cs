using Fdw.Services.Abstractions;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.EnvironmentVariable.Registration;

/// <summary>Supplies the EnvironmentVariableSecretManager implementation's own configuration.</summary>
public interface IEnvironmentVariableSecretManagerConfigurationProvider
    : IImplementationConfigurationProvider<ISecretManagerImplementationConfiguration>
{
}
