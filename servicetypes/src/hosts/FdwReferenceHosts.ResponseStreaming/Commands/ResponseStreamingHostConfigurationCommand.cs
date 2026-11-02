using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.ResponseStreaming.Configuration;

namespace FdwReferenceHosts.ResponseStreaming.Commands;

/// <summary>Reads and writes the ResponseStreamingHost implementation records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostResponseStreamingConfiguration")]
public sealed class ResponseStreamingHostConfigurationCommand : ConfigurationCommandBase<ResponseStreamingHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ResponseStreamingHostConfigurationCommand"/> class.</summary>
    public ResponseStreamingHostConfigurationCommand()
        : base("HostResponseStreamingConfiguration")
    {
    }
}
