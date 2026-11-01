using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.WebMcp.Logging;

/// <summary>Structured events for the WebMCP host option.</summary>
public static partial class WebMcpHostLog
{
    /// <summary>Logs that the configuration handed to the factory was not a WebMCP record.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="actualType">The type actually supplied.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62021, Level = LogLevel.Error,
        Message = "The WebMCP host factory was given a '{actualType}', not a WebMcpHostConfiguration")]
    public static partial IGenericMessage ConfigurationNotWebMcp(ILogger logger, string actualType);

    /// <summary>Logs that a command was sent to a service that executes nothing.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The command that was sent.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62022, Level = LogLevel.Error,
        Message = "The WebMCP host service carries settings and executes nothing; it was sent a '{commandType}'")]
    public static partial IGenericMessage ServiceExecutesNothing(ILogger logger, string commandType);
}
