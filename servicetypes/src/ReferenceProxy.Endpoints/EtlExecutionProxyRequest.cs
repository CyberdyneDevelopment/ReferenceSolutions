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
/// Request identifying a single ETL execution.
/// </summary>
public sealed class EtlExecutionProxyRequest
{
    /// <summary>Gets or sets the execution identifier.</summary>
    public Guid Id { get; set; }
}
