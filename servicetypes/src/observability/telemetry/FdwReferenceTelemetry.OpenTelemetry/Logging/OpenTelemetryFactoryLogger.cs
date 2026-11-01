using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceTelemetry.OpenTelemetry.Logging;

/// <summary>Structured log messages for <see cref="OpenTelemetryFactory"/>. EventIds 62201-62202.</summary>
public static partial class OpenTelemetryFactoryLogger
{
    /// <summary>Logs that the supplied configuration was null.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message describing the failure.</returns>
    [MessageLogging(EventId = 62201, Level = LogLevel.Error,
        Message = "OpenTelemetry configuration was null")]
    public static partial IGenericMessage ConfigurationNull(ILogger<OpenTelemetryFactory> logger);

    /// <summary>Logs that a host configuration was resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The configuration name.</param>
    /// <param name="serviceName">The service name reported on traces.</param>
    /// <returns>The message describing the outcome.</returns>
    [MessageLogging(EventId = 62202, Level = LogLevel.Information,
        Message = "telemetry pipeline '{configurationName}' resolved with service name '{serviceName}'")]
    public static partial IGenericMessage HostResolved(
        ILogger<OpenTelemetryFactory> logger, string configurationName, string serviceName);
}
