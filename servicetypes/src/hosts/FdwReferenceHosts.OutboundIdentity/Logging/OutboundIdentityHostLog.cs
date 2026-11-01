using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.OutboundIdentity.Logging;

/// <summary>Structured events for the outbound-identity host option.</summary>
public static partial class OutboundIdentityHostLog
{
    /// <summary>Logs that a command was sent to a service that executes nothing.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The command that was sent.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62301, Level = LogLevel.Error,
        Message = "The outbound-identity host service carries its settings and executes nothing; it was sent a '{commandType}'")]
    public static partial IGenericMessage ServiceExecutesNothing(ILogger logger, string commandType);

    /// <summary>Logs the outbound identity a host resolved from its configuration.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="identityName">The <c>sec.Identity</c> row the host authenticates as.</param>
    /// <param name="audience">The audience its tokens are requested for.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 62302, Level = LogLevel.Information,
        Message = "Outbound identity resolved from configuration: authenticates as '{identityName}' for audience '{audience}'")]
    public static partial IGenericMessage IdentityResolved(ILogger logger, string identityName, string audience);
}
