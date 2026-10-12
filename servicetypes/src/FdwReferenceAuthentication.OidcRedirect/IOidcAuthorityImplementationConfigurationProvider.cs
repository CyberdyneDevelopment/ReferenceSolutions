using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Abstractions;

namespace FdwReferenceAuthentication.OidcRedirect;

/// <summary>Supplies the OidcAuthority implementation's own configuration to the domain that registers it.</summary>
public interface IOidcAuthorityImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IOidcAuthorityImplementationConfiguration>
{
}
