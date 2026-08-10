using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Ui.Logging;

/// <summary>
/// MessageLogging methods for error page operations.
/// EventId range: 9710-9719
/// </summary>
public static partial class ErrorPageLog
{
    [MessageLogging(EventId = 9710, Level = LogLevel.Error, Message = "Unhandled exception on path '{path}' (requestId={requestId})")]
    public static partial IGenericMessage UnhandledException(ILogger logger, Exception ex, string requestId, string path);

    [MessageLogging(EventId = 9711, Level = LogLevel.Warning, Message = "Error page rendered without exception context (requestId={requestId})")]
    public static partial IGenericMessage NoExceptionContext(ILogger logger, string requestId);

    [MessageLogging(EventId = 9712, Level = LogLevel.Warning, Message = "Error page rendered without HttpContext")]
    public static partial IGenericMessage HttpContextUnavailable(ILogger logger);
}
