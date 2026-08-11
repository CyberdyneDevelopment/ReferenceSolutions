using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReferenceProxy.Endpoints.Logging;

namespace ReferenceProxy.Endpoints;

/// <summary>
/// Response for ETL webhook acknowledgement.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EtlWebhookResponse
{
    /// <summary>
    /// Gets or sets whether the webhook was acknowledged.
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// Gets or sets the execution ID that was processed.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;
}
