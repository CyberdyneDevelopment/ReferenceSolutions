using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.ForwardedHeaders.Configuration;

namespace FdwReferenceHosts.ForwardedHeaders.Commands;

/// <summary>Reads and writes the ForwardedHeadersTrustedProxyNetwork child records.</summary>
/// <remarks>The implementation provider saves its children through their own command; a child with
/// none cannot be saved at all.</remarks>
[TypeOption(typeof(ConfigurationCommands), "ForwardedHeadersTrustedProxyNetwork")]
public sealed class ForwardedHeadersTrustedProxyNetworkConfigurationCommand : ConfigurationCommandBase<ForwardedHeadersTrustedProxyNetwork>
{
    /// <summary>Initializes a new instance of the <see cref="ForwardedHeadersTrustedProxyNetworkConfigurationCommand"/> class.</summary>
    public ForwardedHeadersTrustedProxyNetworkConfigurationCommand()
        : base("ForwardedHeadersTrustedProxyNetwork")
    {
    }
}
