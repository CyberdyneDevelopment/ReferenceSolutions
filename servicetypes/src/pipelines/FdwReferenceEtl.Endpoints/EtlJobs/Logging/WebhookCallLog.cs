using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtlJobs.Endpoints.Logging;

/// <summary>
/// MessageLogging for calls to a webhook trigger (FDW-1884). A refused call is logged with its reason here and answered to the caller
/// with nothing but the status, so a caller cannot learn which names exist.
/// EventId range: 10370-10379.
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class WebhookCallLog
{
    /// <summary>Logs a call that is refused with 401, and why.</summary>
    [MessageLogging(EventId = 10370, Level = LogLevel.Warning, Message = "A call to webhook '{webhook}' was refused: {reason}")]
    public static partial IGenericMessage Refused(ILogger logger, string webhook, string reason);

    /// <summary>Logs a call that is accepted.</summary>
    [MessageLogging(EventId = 10371, Level = LogLevel.Information, Message = "Webhook '{webhook}' called; starting pipeline '{pipeline}'")]
    public static partial IGenericMessage Accepted(ILogger logger, string webhook, string pipeline);

    /// <summary>Logs a call whose body is refused after the secret was accepted.</summary>
    [MessageLogging(EventId = 10372, Level = LogLevel.Warning, Message = "A call to webhook '{webhook}' was refused after its secret was accepted: {reason}")]
    public static partial IGenericMessage BodyRefused(ILogger logger, string webhook, string reason);

    /// <summary>Logs a webhook trigger that could not be read.</summary>
    [MessageLogging(EventId = 10373, Level = LogLevel.Error, Message = "Webhook '{webhook}' could not be read: {reason}")]
    public static partial IGenericMessage Unreadable(ILogger logger, string webhook, string reason);
}
