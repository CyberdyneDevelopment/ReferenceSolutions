using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceLogging.Serilog.Commands;

/// <summary>Data commands for the Serilog implementation table.</summary>
[TypeOption(typeof(ConfigurationCommands), "SerilogLoggingConfiguration")]
public sealed class SerilogLoggingConfigurationCommand
    : ConfigurationCommandBase<SerilogLoggingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SerilogLoggingConfigurationCommand"/> class.</summary>
    public SerilogLoggingConfigurationCommand() : base("SerilogLoggingConfiguration") { }
}
