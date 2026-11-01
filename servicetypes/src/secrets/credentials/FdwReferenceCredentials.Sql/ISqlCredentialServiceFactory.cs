using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Sql.Configuration;

namespace FdwReferenceCredentials.Sql;

/// <summary>
/// The factory for the SQL credential service.
/// </summary>
/// <remarks>
/// Its own type, so the container can tell this implementation's factory from every other one in
/// the domain. Registering them all under the domain's own closed generic gave each implementation
/// the same service type, and TryAdd keeps the first -- so whichever option initialised first was
/// the only one that could ever be resolved.
/// </remarks>
public interface ISqlCredentialServiceFactory : ICredentialServiceFactory<ICredentialService, SqlCredentialServiceConfiguration>
{
}
