using Fdw.Collections.Attributes;

namespace FdwReferenceSerilog.Levels;

/// <summary>The Serilog Error level: a failure of the work at hand.</summary>
[TypeOption(typeof(SerilogLevels), "Error")]
public sealed class ErrorLevel : SerilogLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="ErrorLevel"/> class.</summary>
    public ErrorLevel()
        : base(5, "Error", global::Serilog.Events.LogEventLevel.Error)
    {
    }
}
