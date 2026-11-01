using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.OutboundIdentity.Configuration;

namespace FdwReferenceHosts.OutboundIdentity.Commands;

/// <summary>Reads and writes the OutboundIdentityHost implementation records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostOutboundIdentityConfiguration")]
public sealed class OutboundIdentityHostConfigurationCommand : ConfigurationCommandBase<OutboundIdentityHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OutboundIdentityHostConfigurationCommand"/> class.</summary>
    public OutboundIdentityHostConfigurationCommand()
        : base("HostOutboundIdentityConfiguration")
    {
    }
}
