using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace FdwReferenceSerilog.Levels;

/// <summary>
/// The levels a Serilog logging row can state (FDW-4153). A row's MinimumLevel and each override's Level is one of these names;
/// <c>ByName</c> answers the option, or <c>NotFound</c> for a name that is none of them.
/// </summary>
/// <remarks>
/// Why this namespace and not FdwReferenceLogging.Serilog.Levels: the generated collection names the option type's
/// <c>Serilog.Events.LogEventLevel</c> without <c>global::</c>, and inside FdwReferenceLogging.Serilog that resolves to this assembly's
/// own namespace, not the Serilog library.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SerilogLevelBase), typeof(ISerilogLevel), typeof(SerilogLevels))]
public abstract partial class SerilogLevels : TypeCollectionBase<SerilogLevelBase, ISerilogLevel>
{
}
