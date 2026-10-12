using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace FdwReferenceTelemetry.OpenTelemetry.Registration;

/// <summary>
/// MessageLogging for the OpenTelemetry option's registration.
/// EventId range: 11032.
/// </summary>
public static partial class OpenTelemetryLog
{
    /// <summary>The host declares no Telemetry row.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The domain row name that was looked up.</param>
    /// <returns>The logged message.</returns>
    /// <remarks>
    /// A phase result rather than a throw from inside a builder callback: what this host reports,
    /// and where, is configuration rather than a default, and a failed phase is how this framework
    /// says so.
    /// </remarks>
    [MessageLogging(
        EventId = 11032,
        Level = LogLevel.Critical,
        Message = "No Telemetry row named '{name}' is configured; what this host reports, and where, is configuration rather than a default")]
    public static partial IGenericMessage TelemetryRowNotConfigured(ILogger logger, string name);
}
