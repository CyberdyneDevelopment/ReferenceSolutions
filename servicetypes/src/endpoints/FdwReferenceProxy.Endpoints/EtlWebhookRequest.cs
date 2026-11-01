using System.Diagnostics.CodeAnalysis;
using System;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Request for receiving ETL webhook completion callbacks.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EtlWebhookRequest
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the completion timestamp.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
