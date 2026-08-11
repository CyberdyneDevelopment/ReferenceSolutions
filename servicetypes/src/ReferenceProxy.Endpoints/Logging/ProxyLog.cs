using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceProxy.Endpoints.Logging;

/// <summary>
/// MessageLogging for API gateway proxy operations.
/// EventId range: 1900-1950
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ProxyLog
{
    [MessageLogging(EventId = 1900, Level = LogLevel.Information, Message = "Proxying {method} to {service}: {path}")]
    public static partial IGenericMessage ProxyRequest(ILogger logger, string method, string service, string path);

    [MessageLogging(EventId = 1901, Level = LogLevel.Information, Message = "Proxy response from {service}: {statusCode}")]
    public static partial IGenericMessage ProxyResponse(ILogger logger, string service, int statusCode);

    // Why: Error — the proxy call could not complete and the caller receives a 502.
    [MessageLogging(EventId = 1902, Level = LogLevel.Error, Message = "Proxy call to {service} failed: {error}")]
    public static partial IGenericMessage ProxyFailed(ILogger logger, string service, string error);

    // Why: Warning, not Error — the circuit breaker opening is the resiliency mechanism doing its
    // job, not a failure in itself. NOTE: zero call sites found (dead declaration) — flagged, not removed.
    [MessageLogging(EventId = 1903, Level = LogLevel.Warning, Message = "Proxy circuit breaker open for {service}")]
    public static partial IGenericMessage ProxyCircuitBreakerOpen(ILogger logger, string service);

    [MessageLogging(EventId = 1904, Level = LogLevel.Information, Message = "ETL webhook received: execution {executionId} status {status}")]
    public static partial IGenericMessage EtlWebhookReceived(ILogger logger, string executionId, string status);

    [MessageLogging(EventId = 1905, Level = LogLevel.Warning, Message = "ETL webhook received for unknown execution {executionId}")]
    public static partial IGenericMessage EtlWebhookUnknownExecution(ILogger logger, string executionId);
}
