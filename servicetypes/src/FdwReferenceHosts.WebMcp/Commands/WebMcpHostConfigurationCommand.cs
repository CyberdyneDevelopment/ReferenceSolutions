using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.WebMcp.Configuration;

namespace FdwReferenceHosts.WebMcp.Commands;

/// <summary>Reads and writes the WebMCP implementation records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostWebMcpConfiguration")]
public sealed class WebMcpHostConfigurationCommand : ConfigurationCommandBase<WebMcpHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WebMcpHostConfigurationCommand"/> class.</summary>
    public WebMcpHostConfigurationCommand()
        : base("HostWebMcpConfiguration")
    {
    }
}
