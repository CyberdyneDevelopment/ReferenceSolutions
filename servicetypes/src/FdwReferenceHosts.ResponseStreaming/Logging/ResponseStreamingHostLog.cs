using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.ResponseStreaming.Logging;

/// <summary>Structured events for the response-streaming host option.</summary>
public static partial class ResponseStreamingHostLog
{
    /// <summary>Logs that a command was sent to a service that executes nothing.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The command that was sent.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62102, Level = LogLevel.Error,
        Message = "The response-streaming host service carries its limits and executes nothing; it was sent a '{commandType}'")]
    public static partial IGenericMessage ServiceExecutesNothing(ILogger logger, string commandType);

    /// <summary>Logs the streaming limits a host resolved from its configuration.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="maxRows">The configured row ceiling.</param>
    /// <param name="maxConcurrentStreams">The configured concurrent-stream limit.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62103, Level = LogLevel.Information,
        Message = "Streaming limits resolved from configuration: {maxRows} rows per response, {maxConcurrentStreams} concurrent streams per endpoint")]
    public static partial IGenericMessage LimitsResolved(ILogger logger, long maxRows, int maxConcurrentStreams);
}
