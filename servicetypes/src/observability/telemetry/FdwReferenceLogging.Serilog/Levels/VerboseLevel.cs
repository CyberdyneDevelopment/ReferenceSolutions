using Fdw.Collections.Attributes;

namespace FdwReferenceSerilog.Levels;

/// <summary>The Serilog Verbose level: everything, including tracing detail.</summary>
[TypeOption(typeof(SerilogLevels), "Verbose")]
public sealed class VerboseLevel : SerilogLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="VerboseLevel"/> class.</summary>
    public VerboseLevel()
        : base(1, "Verbose", global::Serilog.Events.LogEventLevel.Verbose)
    {
    }
}
