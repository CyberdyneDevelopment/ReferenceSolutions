using Fdw.Collections.Attributes;

namespace FdwReferenceSerilog.Levels;

/// <summary>The Serilog Information level: normal operation.</summary>
[TypeOption(typeof(SerilogLevels), "Information")]
public sealed class InformationLevel : SerilogLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="InformationLevel"/> class.</summary>
    public InformationLevel()
        : base(3, "Information", global::Serilog.Events.LogEventLevel.Information)
    {
    }
}
