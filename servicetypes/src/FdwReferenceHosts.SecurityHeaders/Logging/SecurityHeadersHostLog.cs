using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.SecurityHeaders.Logging;

/// <summary>Structured events for the CORS host option.</summary>
public static partial class SecurityHeadersHostLog
{
    /// <summary>Logs that the configuration handed to the factory was not a CORS record.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="actualType">The type actually supplied.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62001, Level = LogLevel.Error,
        Message = "The CORS host factory was given a '{actualType}', not a SecurityHeadersHostConfiguration")]
    public static partial IGenericMessage ConfigurationNotSecurityHeaders(ILogger logger, string actualType);

    /// <summary>Logs that a command was sent to a service that executes nothing.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The command that was sent.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62002, Level = LogLevel.Error,
        Message = "The CORS host service carries a policy and executes nothing; it was sent a '{commandType}'")]
    public static partial IGenericMessage ServiceExecutesNothing(ILogger logger, string commandType);
}
