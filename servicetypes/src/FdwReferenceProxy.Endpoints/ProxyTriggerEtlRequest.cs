using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Request for triggering an ETL job via proxy.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ProxyTriggerEtlRequest
{
    /// <summary>
    /// Gets or sets the pipeline name to execute.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the trigger source identifier.
    /// </summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets the run parameter values (contract item 3): each name must be a parameter the pipeline declares, each
    /// required parameter must be given, and each value is parsed by the declared type.
    /// </summary>
    public IList<PipelineParameterValue>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the roles the run acts with (FDW-1161), by name. Required: at least one, only roles the caller holds in the
    /// tenant it acts in, and the roles must be able to reach everything the pipeline reads and writes.
    /// </summary>
    public IList<string>? RunRoles { get; set; }

    /// <summary>
    /// Gets or sets how many rows each source may read in a test run (FDW-1676). When set, the run is a test run: each source stops
    /// after this many rows, no destination is written and no step with an outside effect runs. When null, the run is a real run.
    /// </summary>
    public int? TestRowsPerSource { get; set; }
}
