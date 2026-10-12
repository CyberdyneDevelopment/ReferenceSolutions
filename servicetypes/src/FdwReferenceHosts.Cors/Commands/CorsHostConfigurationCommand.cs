using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.Cors.Configuration;

namespace FdwReferenceHosts.Cors.Commands;

/// <summary>Reads and writes the CORS implementation records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostCorsConfiguration")]
public sealed class CorsHostConfigurationCommand : ConfigurationCommandBase<CorsHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CorsHostConfigurationCommand"/> class.</summary>
    public CorsHostConfigurationCommand()
        : base("HostCorsConfiguration")
    {
    }
}
