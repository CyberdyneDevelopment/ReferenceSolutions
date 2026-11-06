using Fdw.Collections.Attributes;

namespace FdwReferenceSerilog.Levels;

/// <summary>The Serilog Debug level: internal detail useful when diagnosing.</summary>
[TypeOption(typeof(SerilogLevels), "Debug")]
public sealed class DebugLevel : SerilogLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="DebugLevel"/> class.</summary>
    public DebugLevel()
        : base(2, "Debug", global::Serilog.Events.LogEventLevel.Debug)
    {
    }
}
