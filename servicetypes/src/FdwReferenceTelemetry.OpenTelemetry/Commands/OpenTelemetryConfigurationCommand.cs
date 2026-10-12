using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceTelemetry.OpenTelemetry.Commands;

/// <summary>Data commands for the ASP.NET Core hosting implementation table.</summary>
[TypeOption(typeof(ConfigurationCommands), "OpenTelemetryConfiguration")]
public sealed class OpenTelemetryConfigurationCommand
    : ConfigurationCommandBase<OpenTelemetryConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryConfigurationCommand"/> class.</summary>
    public OpenTelemetryConfigurationCommand() : base("OpenTelemetryConfiguration") { }
}
