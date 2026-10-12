using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.OidcRedirect;

/// <summary>
/// MessageLogging for the OIDC redirect step.
/// </summary>
/// <remarks>
/// No code, state, verifier, nonce, client secret or token appears at any level — each is either a
/// credential or a means of completing someone else's exchange.
/// <para>
/// Lives here rather than in FDW core: reference implementations get no InternalsVisibleTo, so
/// anything <c>OidcRedirectStepType</c> (in the sibling <c>.Registration</c> assembly) needs has to
/// be public API this package owns, not an internal class in a framework package it merely
/// references. This class was originally FDW-core's own (left behind when the step itself moved out
/// during the FDW-672 option-is-the-step conversion); moved here rather than made public in FDW,
/// since it is exclusively OIDC-vendor-specific and belongs with the step that uses it.
/// </para>
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
public static partial class OidcLog
{
    /// <summary>The caller is being sent to the provider.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91210, Level = LogLevel.Trace,
        Message = "Challenging to '{issuer}'")]
    public static partial IGenericMessage Challenging(ILogger logger, string issuer);

    /// <summary>The caller came back and their token verified.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91211, Level = LogLevel.Trace,
        Message = "Completed the exchange with '{issuer}'")]
    public static partial IGenericMessage Completed(ILogger logger, string issuer);

    /// <summary>The callback carried a code but no state.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91212, Level = LogLevel.Warning,
        Message = "A callback from '{issuer}' carried no state and cannot be matched to a request")]
    public static partial IGenericMessage StateMissing(ILogger logger, string issuer);

    /// <summary>The stored request was made to a different provider.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="expected">The provider this step is configured for.</param>
    /// <param name="actual">The provider the request was made to.</param>
    [MessageLogging(EventId = 91213, Level = LogLevel.Warning,
        Message = "A request made to '{actual}' was returned to the step for '{expected}'")]
    public static partial IGenericMessage IssuerMismatch(
        ILogger logger, string expected, string actual);

    /// <summary>The provider refused the code exchange.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="statusCode">What it answered.</param>
    [MessageLogging(EventId = 91214, Level = LogLevel.Warning,
        Message = "'{issuer}' refused the code exchange with status {statusCode}")]
    public static partial IGenericMessage ExchangeRefused(
        ILogger logger, string issuer, string statusCode);

    /// <summary>The client secret was read for a code exchange.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="secretManagerName">The secret manager it was read from.</param>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "Client secret for '{issuer}' resolved from secret manager '{secretManagerName}'")]
    public static partial IGenericMessage ClientSecretResolved(
        ILogger logger, string issuer, string secretManagerName);

    /// <summary>The secret the authority names holds no value.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="secretName">The key the authority names.</param>
    /// <param name="secretManagerName">The secret manager it was read from.</param>
    [MessageLogging(EventId = 60000, Level = LogLevel.Error,
        Message = "Client secret '{secretName}' for '{issuer}' holds no value in secret manager '{secretManagerName}', so the token endpoint cannot authenticate this host")]
    public static partial IGenericMessage ClientSecretEmpty(
        ILogger logger, string issuer, string secretName, string secretManagerName);

    /// <summary>The token endpoint could not be reached.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="failure">The kind of failure.</param>
    [MessageLogging(EventId = 91215, Level = LogLevel.Error,
        Message = "Could not reach the token endpoint at '{issuer}': {failure}")]
    public static partial IGenericMessage ExchangeFailed(
        ILogger logger, string issuer, string failure);

    /// <summary>The exchange succeeded but returned no identity token.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91216, Level = LogLevel.Error,
        Message = "'{issuer}' returned no id_token — check that the openid scope is requested")]
    public static partial IGenericMessage NoIdToken(ILogger logger, string issuer);

    /// <summary>The returned token failed validation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="failure">The kind of check that failed.</param>
    [MessageLogging(EventId = 91217, Level = LogLevel.Warning,
        Message = "A token from '{issuer}' was rejected: {failure}")]
    public static partial IGenericMessage TokenRejected(
        ILogger logger, string issuer, string failure);

    /// <summary>The token did not echo the nonce this platform sent.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91218, Level = LogLevel.Warning,
        Message = "A token from '{issuer}' did not echo the nonce for this request")]
    public static partial IGenericMessage NonceMismatch(ILogger logger, string issuer);

    /// <summary>The token carried no subject under the configured claim.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="claim">The claim that was expected.</param>
    [MessageLogging(EventId = 91219, Level = LogLevel.Error,
        Message = "A token from '{issuer}' carried no '{claim}' claim to bind on")]
    public static partial IGenericMessage SubjectClaimMissing(
        ILogger logger, string issuer, string claim);

    /// <summary>The step ran before its Initialize phase captured what it needs.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step the flow named.</param>
    [MessageLogging(EventId = 91220, Level = LogLevel.Error,
        Message = "Step '{stepName}' ran before its dependencies were captured, so it cannot do its work. The option's Initialize phase did not run, which means the host was not fully initialized before a flow reached this step")]
    public static partial IGenericMessage NotInitialized(ILogger logger, string stepName);
}
