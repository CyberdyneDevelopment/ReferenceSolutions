using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReferenceProxy.Endpoints.Logging;

namespace ReferenceProxy.Endpoints;

/// <summary>
/// Request for triggering an orchestration node by type.
/// </summary>
public sealed class TriggerOrchestrationNodeProxyRequest
{
    /// <summary>Gets or sets the orchestration node type to trigger.</summary>
    public string Type { get; set; } = string.Empty;
}
