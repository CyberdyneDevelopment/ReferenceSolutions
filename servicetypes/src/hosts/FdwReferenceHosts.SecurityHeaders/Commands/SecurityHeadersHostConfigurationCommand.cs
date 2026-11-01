using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.SecurityHeaders.Configuration;

namespace FdwReferenceHosts.SecurityHeaders.Commands;

/// <summary>Reads and writes the SecurityHeadersHost implementation records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostSecurityHeadersConfiguration")]
public sealed class SecurityHeadersHostConfigurationCommand : ConfigurationCommandBase<SecurityHeadersHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SecurityHeadersHostConfigurationCommand"/> class.</summary>
    public SecurityHeadersHostConfigurationCommand()
        : base("HostSecurityHeadersConfiguration")
    {
    }
}
