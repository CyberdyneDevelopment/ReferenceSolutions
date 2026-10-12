using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceLogging.Serilog;

/// <summary>
/// One per-source minimum level — the <c>Serilog:MinimumLevel:Override</c> entries.
/// </summary>
/// <remarks>
/// A child of <see cref="SerilogLoggingConfiguration"/> rather than a dictionary column: the six
/// overrides reference-api ships (<c>Microsoft</c>, <c>System</c>, <c>OpenTelemetry</c> and friends)
/// are rows an operator edits individually, and a serialised map cannot be queried or diffed.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Logging", ServiceType = "SerilogLevelOverride")]
public sealed partial class SerilogLevelOverride : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }


    /// <summary>Gets or sets the source context prefix this level applies to.</summary>
    public string SourceContext { get; set; } = string.Empty;

    /// <summary>Gets or sets the Serilog level name emitted for that source.</summary>
    public string Level { get; set; } = string.Empty;

}
