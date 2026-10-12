using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceAuthentication.ForeignToken;

/// <summary>
/// Reads and writes the foreign-authority rows of the <c>auth</c> path.
/// </summary>
/// <remarks>
/// The string passed to the base IS the container queried, so it must match the container the host
/// declares. Nothing checks the two against each other, and a mismatch reads as an authority that
/// does not exist rather than as a naming error.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "ForeignAuthority")]
public sealed class ForeignAuthorityConfigurationCommand
    : ConfigurationCommandBase<ForeignAuthorityConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ForeignAuthorityConfigurationCommand"/> class.</summary>
    public ForeignAuthorityConfigurationCommand()
        : base("ForeignAuthority")
    {
    }
}
