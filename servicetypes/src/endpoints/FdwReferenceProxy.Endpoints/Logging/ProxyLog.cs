using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints.Logging;

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

    // Why: Error — the downstream call failed; the caller receives the status ResultHttpStatusMapper maps
    // from the result code (the upstream status for an HTTP failure, 502 when unreachable). The error is
    // the result's own message, null when it carried none (FDW-899).
    [MessageLogging(EventId = 1902, Level = LogLevel.Error, Message = "Proxy call to {service} failed: {error}")]
    public static partial IGenericMessage ProxyFailed(ILogger logger, string service, string? error);

    // Why: Warning, not Error — the circuit breaker opening is the resiliency mechanism doing its
    // job, not a failure in itself. NOTE: zero call sites found (dead declaration) — flagged, not removed.
    [MessageLogging(EventId = 1903, Level = LogLevel.Warning, Message = "Proxy circuit breaker open for {service}")]
    public static partial IGenericMessage ProxyCircuitBreakerOpen(ILogger logger, string service);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70001, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage CreateGraphProxyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70002, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage TriggerOrchestrationNodeProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70003, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage GetGraphProxyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70004, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage GetEtlExecutionStatusProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70005, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage CreateGraphFromTemplateProxyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70006, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage ValidateGraphProxyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70007, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage UpdateGraphProxyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70008, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage GetPipelineDetailProxyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70009, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage ListPipelineTemplatesProxyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70010, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage ListExecutionsProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70011, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage DescribeGraphProxyReturnedNothing(ILogger logger);

    /// <summary>Logs that Schedule proxy was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Schedule proxy was not found, or is not visible to the caller", ClientMessage = "Schedule proxy was not found")]
    public static partial IGenericMessage GetScheduleProxyScheduleproxyNotFound(ILogger logger);

    /// <summary>Logs that Toggle schedule proxy was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "Toggle schedule proxy was not found, or is not visible to the caller", ClientMessage = "Toggle schedule proxy was not found")]
    public static partial IGenericMessage ToggleScheduleProxyTogglescheduleproxyNotFound(ILogger logger);
}
