using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.Cors.Configuration;

namespace FdwReferenceHosts.Cors.Commands;

/// <summary>Reads and writes the CorsMethod child records.</summary>
/// <remarks>The implementation provider saves its children through their own command; a child with
/// none cannot be saved at all.</remarks>
[TypeOption(typeof(ConfigurationCommands), "CorsMethod")]
public sealed class CorsMethodConfigurationCommand : ConfigurationCommandBase<CorsMethod>
{
    /// <summary>Initializes a new instance of the <see cref="CorsMethodConfigurationCommand"/> class.</summary>
    public CorsMethodConfigurationCommand()
        : base("CorsMethod")
    {
    }
}
