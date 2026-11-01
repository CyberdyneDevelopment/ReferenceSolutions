using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.AuthorizeIssuance.Registration;

/// <summary>
/// MessageLogging for deciding whether a token may be issued for an established principal.
/// </summary>
/// <remarks>
/// EventId range: 91256. The step-ran-before-Initialize failure is not here -- every step reports
/// that through <c>AuthenticationStepTypeBase.NotInitialized</c> with the same code, so a step in
/// another package never needs its own copy.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class AuthorizeIssuanceLog
{
    /// <summary>Eligibility for issuance was decided.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">The principal.</param>
    /// <param name="permitted">Whether issuance is permitted.</param>
    /// <param name="reason">Why.</param>
    [MessageLogging(EventId = 91256, Level = LogLevel.Trace,
        Message = "Principal {principalId} issuance permitted={permitted}: {reason}")]
    internal static partial IGenericMessage EligibilityDecided(
        ILogger logger, Guid principalId, bool permitted, string reason);
}
