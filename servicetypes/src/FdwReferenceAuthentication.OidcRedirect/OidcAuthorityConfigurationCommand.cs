using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceAuthentication.OidcRedirect;

/// <summary>
/// Reads and writes the OIDC-authority rows of the <c>auth</c> path.
/// </summary>
/// <remarks>
/// The string passed to the base IS the container queried, so it must match the container the host
/// declares. Nothing checks the two against each other, and a mismatch reads as a provider that does
/// not exist rather than as a naming error.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "OidcAuthority")]
public sealed class OidcAuthorityConfigurationCommand
    : ConfigurationCommandBase<OidcAuthorityConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OidcAuthorityConfigurationCommand"/> class.</summary>
    public OidcAuthorityConfigurationCommand()
        : base("OidcAuthority")
    {
    }
}
