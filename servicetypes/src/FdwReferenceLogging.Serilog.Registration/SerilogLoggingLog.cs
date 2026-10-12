using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace FdwReferenceLogging.Serilog.Registration;

/// <summary>
/// MessageLogging for the Serilog logging option's registration.
/// EventId range: 11030-11031.
/// </summary>
public static partial class SerilogLoggingLog
{
    /// <summary>The host declares no Logging row under this option's name.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The option name the row was looked up under.</param>
    /// <returns>The logged message.</returns>
    /// <remarks>
    /// Fails the phase rather than throwing from inside a DI callback: what this host writes, and
    /// where, is configuration rather than a default, and a phase result is how this framework
    /// reports that.
    /// </remarks>
    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Critical,
        Message = "No Logging row named '{name}' is configured; what this host writes, and where, is configuration rather than a default")]
    public static partial IGenericMessage LoggingRowNotConfigured(ILogger logger, string name);

    /// <summary>The factory did not build a Serilog logger from the configured row.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The option name whose row was used.</param>
    /// <param name="reason">What the factory reported.</param>
    /// <returns>The logged message.</returns>
    [MessageLogging(
        EventId = 11031,
        Level = LogLevel.Critical,
        Message = "The Serilog factory did not build a logger from the '{name}' row: {reason}")]
    public static partial IGenericMessage LoggerNotBuiltFromRow(ILogger logger, string name, string reason);
}
