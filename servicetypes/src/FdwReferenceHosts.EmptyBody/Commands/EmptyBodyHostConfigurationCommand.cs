using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.EmptyBody.Configuration;

namespace FdwReferenceHosts.EmptyBody.Commands;

/// <summary>Reads and writes the EmptyBodyHost implementation records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostEmptyBodyConfiguration")]
public sealed class EmptyBodyHostConfigurationCommand : ConfigurationCommandBase<EmptyBodyHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EmptyBodyHostConfigurationCommand"/> class.</summary>
    public EmptyBodyHostConfigurationCommand()
        : base("HostEmptyBodyConfiguration")
    {
    }
}
