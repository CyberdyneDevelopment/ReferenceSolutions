using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Logging.Abstractions;

namespace FdwReferenceLogging.Serilog;

/// <summary>
/// Serilog's own configuration — what the logging domain record holds when its
/// <c>Implementation</c> is <c>Serilog</c>.
/// </summary>
/// <remarks>
/// Covers what reference-api currently splits across two places: the literals in the stage-1
/// bootstrap logger, and the <c>Serilog</c> section its stage-2
/// <c>ReadFrom.Configuration</c> reads. The stage-1 logger stays literal — it exists to log before
/// any configuration source is reachable, including this one.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Logging", ServiceType = "Serilog")]
public sealed partial class SerilogLoggingConfiguration : ILoggingImplementationConfiguration
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

    /// <summary>Gets or sets the default minimum level — <c>Serilog:MinimumLevel:Default</c>.</summary>
    public string MinimumLevel { get; set; } = string.Empty;

    /// <summary>Gets or sets the per-source level overrides.</summary>
    public IList<SerilogLevelOverride> Overrides { get; set; } = new List<SerilogLevelOverride>();

    /// <summary>Gets or sets the console output template.</summary>
    public string OutputTemplate { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the console sink is written to.</summary>
    public bool WriteToConsole { get; set; }

    /// <summary>Gets or sets the Grafana Loki ingestion URI, when that sink is wanted.</summary>
    public string? LokiUri { get; set; }

    /// <summary>Gets or sets the Loki label identifying this application.</summary>
    public string? LokiApplicationLabel { get; set; }

    /// <summary>Gets or sets the Seq ingestion URL, when that sink is wanted.</summary>
    public string? SeqServerUrl { get; set; }

    /// <summary>Gets or sets the rolling file path, when a file sink is wanted.</summary>
    public string? FilePath { get; set; }

    // Why a switch and not a path: the sink resolves the App Service diagnostics location from the
    // environment it is running in, so there is nothing to configure beyond whether it runs at all.
    /// <summary>Gets or sets a value indicating whether the Azure App Service diagnostics sink is written to.</summary>
    public bool WriteToAzureAppService { get; set; }

    /// <summary>Gets or sets a value indicating whether <c>FromLogContext</c> enrichment is applied.</summary>
    public bool EnrichFromLogContext { get; set; }

    /// <summary>Gets or sets a value indicating whether trace-span enrichment is applied.</summary>
    public bool EnrichWithSpan { get; set; }

    /// <summary>Gets or sets a value indicating whether machine-name enrichment is applied.</summary>
    public bool EnrichWithMachineName { get; set; }

    /// <summary>Gets or sets a value indicating whether environment-name enrichment is applied.</summary>
    public bool EnrichWithEnvironmentName { get; set; }

    /// <summary>Gets or sets a value indicating whether thread-id enrichment is applied.</summary>
    public bool EnrichWithThreadId { get; set; }

    /// <summary>Gets or sets a value indicating whether process-id enrichment is applied.</summary>
    public bool EnrichWithProcessId { get; set; }

    /// <summary>Gets or sets the <c>Application</c> property stamped on every event.</summary>
    public string? ApplicationName { get; set; }
}
