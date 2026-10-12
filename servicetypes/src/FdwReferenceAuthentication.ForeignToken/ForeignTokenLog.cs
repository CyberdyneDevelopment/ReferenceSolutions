using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.ForeignToken;

/// <summary>
/// MessageLogging for accepting a token another authority issued.
/// </summary>
/// <remarks>
/// EventId range: 91160–91164. The token never appears, at any level. Neither does the subject it
/// names — that identifies a person at an external authority, and the issuer alone is enough to say
/// which trust relationship is involved.
/// <para>
/// Lives here rather than in FDW core: reference implementations get no InternalsVisibleTo, so
/// anything <c>ForeignTokenStepType</c> (in the sibling <c>.Registration</c> assembly) needs has to
/// be public API this package owns, not an internal class in a framework package it merely
/// references. This class was originally FDW-core's own (left behind when the step itself moved out
/// during the FDW-672 option-is-the-step conversion); moved here rather than made public in FDW,
/// since it is exclusively foreign-authority-specific and belongs with the step that uses it.
/// </para>
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
public static partial class ForeignTokenLog
{
    /// <summary>A token from a trusted authority passed every check.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that issued it.</param>
    [MessageLogging(EventId = 91160, Level = LogLevel.Trace,
        Message = "A token issued by '{issuer}' was accepted")]
    public static partial IGenericMessage Accepted(ILogger logger, string issuer);

    /// <summary>No token was presented for a step that requires one.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority the flow expected one from.</param>
    [MessageLogging(EventId = 91161, Level = LogLevel.Warning,
        Message = "No token was presented for the flow expecting one issued by '{issuer}'")]
    public static partial IGenericMessage NoTokenPresented(
        ILogger logger, string issuer);

    /// <summary>A presented token failed validation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority it claimed.</param>
    /// <param name="failure">The kind of check that failed.</param>
    [MessageLogging(EventId = 91162, Level = LogLevel.Warning,
        Message = "A token claiming issuer '{issuer}' was rejected: {failure}")]
    public static partial IGenericMessage Rejected(
        ILogger logger, string issuer, string failure);

    /// <summary>A valid token carried no subject.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that issued it.</param>
    [MessageLogging(EventId = 91163, Level = LogLevel.Error,
        Message = "A token from '{issuer}' verified but carried no subject claim")]
    public static partial IGenericMessage NoSubjectClaim(
        ILogger logger, string issuer);

    /// <summary>The step ran before its Initialize phase captured what it needs.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step the flow named.</param>
    [MessageLogging(EventId = 91164, Level = LogLevel.Error,
        Message = "Step '{stepName}' ran before its dependencies were captured, so it cannot do its work. The option's Initialize phase did not run, which means the host was not fully initialized before a flow reached this step")]
    public static partial IGenericMessage NotInitialized(ILogger logger, string stepName);
}
