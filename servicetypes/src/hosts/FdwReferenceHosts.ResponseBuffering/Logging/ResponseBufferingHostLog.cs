using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.ResponseBuffering.Logging;

/// <summary>Structured events for the response-buffering host option.</summary>
public static partial class ResponseBufferingHostLog
{
    /// <summary>Logs that the configuration handed to the factory was not a response-buffering record.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="actualType">The type actually supplied.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62001, Level = LogLevel.Error,
        Message = "The response-buffering host factory was given a '{actualType}', not a ResponseBufferingHostConfiguration")]
    public static partial IGenericMessage ConfigurationNotResponseBuffering(ILogger logger, string actualType);

    /// <summary>Logs that a command was sent to a service that executes nothing.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The command that was sent.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62002, Level = LogLevel.Error,
        Message = "The response-buffering host service carries its settings and executes nothing; it was sent a '{commandType}'")]
    public static partial IGenericMessage ServiceExecutesNothing(ILogger logger, string commandType);
}
