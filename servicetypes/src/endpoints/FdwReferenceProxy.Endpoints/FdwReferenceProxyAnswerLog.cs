using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>The coded messages the fdwreferenceproxy endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("RSTPROXYANSWERS")]
public static partial class FdwReferenceProxyAnswerLog
{

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70001, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage LookupPipelinesProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70002, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage GetPipelinesProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70003, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage GetExecutionEventsProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70004, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage WebhookProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70005, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage GetExecutionChildrenProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70006, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage DenyEtlExecutionProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70007, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage WebhookCallProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70008, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage GetExecutionTrackingProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70009, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage EtlRelayFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70010, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage ApproveEtlExecutionProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 70011, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage ScriptProxyFailedToProxyRequestTo(ILogger logger);

    /// <summary>Logs that A call proxied to a webhook sent a body larger than 1 MB; it was not forwarded.</summary>
    [MessageLogging(EventId = 130002, Level = LogLevel.Warning, Message = "A call proxied to a webhook sent a body larger than 1 MB; it was not forwarded", ClientMessage = "The body is larger than 1 MB")]
    public static partial IGenericMessage WebhookCallProxyBodyTooLarge(ILogger logger);
}
