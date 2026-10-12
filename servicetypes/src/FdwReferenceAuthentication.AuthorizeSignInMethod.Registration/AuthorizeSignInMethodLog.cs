using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.AuthorizeSignInMethod.Registration;

/// <summary>
/// MessageLogging for deciding whether a principal may use the sign-in method a flow is.
/// </summary>
/// <remarks>
/// EventId range: 91300–91304. The step-ran-before-Initialize failure is not here -- every step
/// reports that through <c>AuthenticationStepTypeBase.NotInitialized</c>.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class AuthorizeSignInMethodLog
{
    /// <summary>The step's settings are not JSON.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">The parser's complaint.</param>
    [MessageLogging(EventId = 91300, Level = LogLevel.Error,
        Message = "AuthorizeSignInMethod settings are not JSON: {reason}. Expected an object naming RequiredPermission")]
    internal static partial IGenericMessage ConfigurationNotJson(ILogger logger, string reason);

    /// <summary>The step's settings name no permission.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91301, Level = LogLevel.Error,
        Message = "AuthorizeSignInMethod settings name no RequiredPermission. A flow open to every active user omits this step instead")]
    internal static partial IGenericMessage PermissionMissing(ILogger logger);

    /// <summary>The principal holds the permission the flow requires.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">The principal.</param>
    /// <param name="permission">The permission.</param>
    [MessageLogging(EventId = 91302, Level = LogLevel.Trace,
        Message = "Principal {principalId} holds '{permission}' and may use this sign-in method")]
    internal static partial IGenericMessage Admitted(ILogger logger, Guid principalId, string permission);

    /// <summary>The principal lacks the permission the flow requires.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">The principal.</param>
    /// <param name="permission">The permission.</param>
    // Information, not Warning: a refusal the operator configured, and one an operator onboarding
    // somebody will look for when that person says they cannot sign in.
    [MessageLogging(EventId = 91303, Level = LogLevel.Information,
        Message = "Principal {principalId} does not hold '{permission}', which this sign-in method requires; issuance is refused")]
    internal static partial IGenericMessage Refused(ILogger logger, Guid principalId, string permission);
}
