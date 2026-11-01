using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceSecretManagers.MsSql.Registration;

/// <summary>The MsSqlSecretManager implementation of its domain.</summary>
public interface IMsSqlSecretManagerProvider : IImplementationServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>
{
}
