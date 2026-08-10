using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Ui.Logging;

/// <summary>
/// Structured logging for page-level lifecycle events.
/// EventId range: 1741-1749
/// </summary>
public static partial class PageLog
{
    /// <summary>Logs that an async operation was cancelled, typically due to navigation or circuit disconnect.</summary>
    [MessageLogging(EventId = 1741, Level = LogLevel.Trace, Message = "Operation cancelled on '{pageName}': {operation}")]
    public static partial IGenericMessage OperationCancelled(ILogger logger, string pageName, string operation);
}
