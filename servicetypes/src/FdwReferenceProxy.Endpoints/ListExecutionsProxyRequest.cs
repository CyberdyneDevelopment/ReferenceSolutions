using System;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Query parameters for listing executions, forwarded verbatim to the ETL server's
/// <c>GET /executions</c> (matches <c>Fdw.Operations.Endpoints.Executions.ListExecutionsRequest</c>).
/// </summary>
public sealed class ListExecutionsProxyRequest
{
    /// <summary>Gets or sets the page number.</summary>
    public int? Page { get; set; }

    /// <summary>Gets or sets the page size.</summary>
    public int? PageSize { get; set; }

    /// <summary>Gets or sets an optional item type filter (Workflow, Job, Stage, Step, Task).</summary>
    public string? ItemType { get; set; }

    /// <summary>Gets or sets an optional state filter.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets an optional correlation ID filter.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets an optional root execution ID filter.</summary>
    public Guid? RootId { get; set; }
}
