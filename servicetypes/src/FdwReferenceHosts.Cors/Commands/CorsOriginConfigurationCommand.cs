using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.Cors.Configuration;

namespace FdwReferenceHosts.Cors.Commands;

/// <summary>Reads and writes the CorsOrigin child records.</summary>
/// <remarks>The implementation provider saves its children through their own command; a child with
/// none cannot be saved at all.</remarks>
[TypeOption(typeof(ConfigurationCommands), "CorsOrigin")]
public sealed class CorsOriginConfigurationCommand : ConfigurationCommandBase<CorsOrigin>
{
    /// <summary>Initializes a new instance of the <see cref="CorsOriginConfigurationCommand"/> class.</summary>
    public CorsOriginConfigurationCommand()
        : base("CorsOrigin")
    {
    }
}
