using Fdw.Collections;

namespace FdwReferenceSerilog.Levels;

/// <summary>
/// Base of the <see cref="SerilogLevels"/> options: each names a level and carries the Serilog level it is.
/// </summary>
public abstract class SerilogLevelBase : TypeOptionBase<SerilogLevelBase>, ISerilogLevel
{
    /// <summary>Initializes a new instance of the <see cref="SerilogLevelBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The name a logging row states.</param>
    /// <param name="serilogLevel">The Serilog level this option is.</param>
    protected SerilogLevelBase(int id, string name, global::Serilog.Events.LogEventLevel serilogLevel)
        : base(id, name)
    {
        SerilogLevel = serilogLevel;
    }

    /// <inheritdoc />
    public global::Serilog.Events.LogEventLevel SerilogLevel { get; }
}
