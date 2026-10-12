using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.ForwardedHeaders.Configuration;

namespace FdwReferenceHosts.ForwardedHeaders.Commands;

/// <summary>Reads and writes the ForwardedHeadersHost implementation records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostForwardedHeadersConfiguration")]
public sealed class ForwardedHeadersHostConfigurationCommand : ConfigurationCommandBase<ForwardedHeadersHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ForwardedHeadersHostConfigurationCommand"/> class.</summary>
    public ForwardedHeadersHostConfigurationCommand()
        : base("HostForwardedHeadersConfiguration")
    {
    }
}
