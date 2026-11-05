using Fdw.Collections;

namespace FdwReferenceSerilog.Levels;

/// <summary>
/// A Serilog level a logging row can name as its MinimumLevel or an override's Level (FDW-4153). The row states the option's <c>Name</c>;
/// nothing parses an enum to find it.
/// </summary>
public interface ISerilogLevel : ITypeOption<int, ISerilogLevel>
{
    /// <summary>Gets the Serilog level this option is, used only where the Serilog pipeline is configured.</summary>
    global::Serilog.Events.LogEventLevel SerilogLevel { get; }
}
