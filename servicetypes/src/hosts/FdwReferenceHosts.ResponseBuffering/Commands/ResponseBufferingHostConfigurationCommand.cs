using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.ResponseBuffering.Configuration;

namespace FdwReferenceHosts.ResponseBuffering.Commands;

/// <summary>Reads and writes the ResponseBufferingHost implementation records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostResponseBufferingConfiguration")]
public sealed class ResponseBufferingHostConfigurationCommand : ConfigurationCommandBase<ResponseBufferingHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ResponseBufferingHostConfigurationCommand"/> class.</summary>
    public ResponseBufferingHostConfigurationCommand()
        : base("HostResponseBufferingConfiguration")
    {
    }
}
