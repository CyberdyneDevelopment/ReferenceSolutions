using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Registration;

/// <summary>The EnvironmentVariableSecretManager implementation of its domain.</summary>
public interface IEnvironmentVariableSecretManagerProvider : IImplementationServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>
{
}
