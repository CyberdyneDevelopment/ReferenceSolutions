using Fdw.Collections.Attributes;

namespace FdwReferenceSerilog.Levels;

/// <summary>The Serilog Fatal level: a failure the application cannot continue after.</summary>
[TypeOption(typeof(SerilogLevels), "Fatal")]
public sealed class FatalLevel : SerilogLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="FatalLevel"/> class.</summary>
    public FatalLevel()
        : base(6, "Fatal", global::Serilog.Events.LogEventLevel.Fatal)
    {
    }
}
