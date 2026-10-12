using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceEndpoints.Configuration;

namespace FdwReferenceEndpoints.Commands;

/// <summary>ConfigurationCommands TypeOption for the API reference page.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostApiReferenceConfiguration")]
public sealed class ApiReferenceHostConfigurationCommand : ConfigurationCommandBase<ApiReferenceHostConfiguration>
{
    /// <inheritdoc/>
    public ApiReferenceHostConfigurationCommand() : base("HostApiReferenceConfiguration") { }
}
