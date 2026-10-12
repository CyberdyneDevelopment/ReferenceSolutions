using Fdw.Messages;

namespace FdwReferenceLogging.Serilog;

/// <summary>Messages produced by <see cref="SerilogLoggingService"/>.</summary>
internal static class SerilogLoggingServiceLogger
{
    /// <summary>Builds the message returned when a command is submitted to a logging pipeline.</summary>
    /// <param name="name">The pipeline name.</param>
    /// <returns>The failure message.</returns>
    internal static IGenericMessage CommandsNotSupported(string name)
        => new GenericMessage(
            MessageSeverity.Error,
            $"Serilog logging pipeline '{name}' has no command surface; Execute is not supported.");
}
