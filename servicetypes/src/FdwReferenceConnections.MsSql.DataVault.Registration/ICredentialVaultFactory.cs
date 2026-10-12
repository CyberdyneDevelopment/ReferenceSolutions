using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Abstractions;

namespace FdwReferenceConnections.MsSql.DataVault.Registration;

/// <summary>
/// The factory for the credential data vault.
/// </summary>
/// <remarks>
/// Its own type, so the container can tell this implementation's factory from every other one in
/// the domain. Registering them all under the domain's own closed generic gave each implementation
/// the same service type, and TryAdd keeps the first -- so whichever option initialised first was
/// the only one that could ever be resolved.
/// </remarks>
public interface ICredentialVaultFactory : IDataVaultFactory<IDataVault, SqlDataVaultConfiguration>
{
}
