using Fdw.Services.Abstractions;
using Fdw.Services.Credentials.Abstractions;

namespace FdwReferenceCredentials.Sql.Registration;

/// <summary>The SqlCredentialService implementation of its domain.</summary>
public interface ISqlCredentialServiceProvider : IImplementationServiceProvider<ICredentialService, ICredentialServiceImplementationConfiguration>
{
}
