using Fdw.Collections.Attributes;

namespace FdwReferenceSerilog.Levels;

/// <summary>The Serilog Warning level: something unexpected that did not stop the work.</summary>
[TypeOption(typeof(SerilogLevels), "Warning")]
public sealed class WarningLevel : SerilogLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="WarningLevel"/> class.</summary>
    public WarningLevel()
        : base(4, "Warning", global::Serilog.Events.LogEventLevel.Warning)
    {
    }
}
