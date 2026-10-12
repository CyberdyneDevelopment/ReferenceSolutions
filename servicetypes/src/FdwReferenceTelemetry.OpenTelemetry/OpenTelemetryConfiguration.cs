using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Telemetry.Abstractions;

namespace FdwReferenceTelemetry.OpenTelemetry;

/// <summary>
/// The telemetry pipeline's own configuration — the request-pipeline settings reference-api
/// currently splits between literals in <c>Program.cs</c> and loose
/// <c>GetSection(...).Get&lt;T&gt;()</c> calls made outside any ServiceType phase.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Telemetry", ServiceType = "AspNetCore")]
public sealed partial class OpenTelemetryConfiguration : ITelemetryImplementationConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the service name reported on every trace and metric.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether tracing is collected.</summary>
    public bool TracingEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether metrics are collected.</summary>
    public bool MetricsEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether traces are also written to the console.</summary>
    public bool ExportToConsole { get; set; }

    /// <summary>Gets or sets the OTLP collector endpoint, when one is used.</summary>
    public string? CollectorEndpoint { get; set; }
}
