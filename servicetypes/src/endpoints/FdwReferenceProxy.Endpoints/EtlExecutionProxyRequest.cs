using System;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Request identifying a single ETL execution.
/// </summary>
public sealed class EtlExecutionProxyRequest
{
    /// <summary>Gets or sets the execution identifier.</summary>
    public Guid Id { get; set; }
}
