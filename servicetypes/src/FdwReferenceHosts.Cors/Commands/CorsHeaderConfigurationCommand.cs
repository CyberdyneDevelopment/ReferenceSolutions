using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.Cors.Configuration;

namespace FdwReferenceHosts.Cors.Commands;

/// <summary>Reads and writes the CorsHeader child records.</summary>
/// <remarks>The implementation provider saves its children through their own command; a child with
/// none cannot be saved at all.</remarks>
[TypeOption(typeof(ConfigurationCommands), "CorsHeader")]
public sealed class CorsHeaderConfigurationCommand : ConfigurationCommandBase<CorsHeader>
{
    /// <summary>Initializes a new instance of the <see cref="CorsHeaderConfigurationCommand"/> class.</summary>
    public CorsHeaderConfigurationCommand()
        : base("CorsHeader")
    {
    }
}
