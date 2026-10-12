using Fdw.Services.Abstractions;
using Fdw.Services.DataVault.Abstractions;

namespace FdwReferenceConnections.MsSql.DataVault.Registration;

/// <summary>The CredentialVault implementation of its domain.</summary>
public interface ICredentialVaultProvider : IImplementationServiceProvider<IDataVault, IDataVaultImplementationConfiguration>
{
}
