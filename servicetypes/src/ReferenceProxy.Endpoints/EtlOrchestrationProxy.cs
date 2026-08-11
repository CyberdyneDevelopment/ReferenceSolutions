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
/// Forwards ETL orchestration calls to the ETL server.
/// </summary>
/// <remarks>
/// <para>
/// Orchestration is executed by the ETL server, which owns the
/// <c>OrchestrationNodeExecutionQueue</c> and the background consumer that drains it. The queue is an
/// in-memory Channel, so a host that enqueues runs the trigger itself; routing every trigger to one
/// server is what makes which host received the request irrelevant to which host executes it.
/// </para>
/// <para>
/// The routes are identical to the ETL server's, so each handler is a straight pass-through. The
/// caller's bearer token rides along via <c>BearerTokenHandler</c> on the named client.
/// </para>
/// </remarks>
internal static class EtlOrchestrationProxy
{
    /// <summary>The named HttpClient registered by PipelineJobClientType from ApiClients:PipelineJobClient:BaseUrl.</summary>
    internal const string ClientName = "PipelineJobClient";
}
