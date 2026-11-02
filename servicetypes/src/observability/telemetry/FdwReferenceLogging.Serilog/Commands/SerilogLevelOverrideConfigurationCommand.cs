using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceLogging.Serilog.Commands;

/// <summary>Reads and writes the SerilogLevelOverride child records.</summary>
/// <remarks>The implementation provider saves its children through their own command; a child with
/// none cannot be saved at all.</remarks>
[TypeOption(typeof(ConfigurationCommands), "SerilogLevelOverride")]
public sealed class SerilogLevelOverrideConfigurationCommand : ConfigurationCommandBase<SerilogLevelOverride>
{
    /// <summary>Initializes a new instance of the <see cref="SerilogLevelOverrideConfigurationCommand"/> class.</summary>
    public SerilogLevelOverrideConfigurationCommand()
        : base("SerilogLevelOverride")
    {
    }
}
