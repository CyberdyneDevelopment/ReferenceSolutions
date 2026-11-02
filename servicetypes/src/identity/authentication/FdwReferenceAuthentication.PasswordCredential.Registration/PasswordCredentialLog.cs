using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.PasswordCredential.Registration;

/// <summary>
/// MessageLogging for proving a caller by password.
/// </summary>
/// <remarks>
/// EventId range: 91240–91242. No password appears at any level, and no message distinguishes an
/// unknown user from a wrong password — the two are one refusal on purpose, so the log cannot be
/// read as an account enumeration oracle either.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class PasswordCredentialLog
{
    /// <summary>A caller proved themselves by password.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">Who they turned out to be.</param>
    [MessageLogging(EventId = 91240, Level = LogLevel.Trace,
        Message = "Password accepted for principal {principalId}")]
    internal static partial IGenericMessage Proved(
        ILogger logger, Guid principalId);

    /// <summary>The credential was refused.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="username">What was presented as a username.</param>
    // Why Information and not Warning: a mistyped password is the single most common event a login
    // endpoint sees. At Warning the ordinary case drowns the genuine ones, and the count that
    // matters - repeated failures against one account - is the credential service's to raise, since
    // it is the only thing that can see the attempt history.
    [MessageLogging(EventId = 91241, Level = LogLevel.Information,
        Message = "Refused the credential presented for '{username}'")]
    internal static partial IGenericMessage Refused(
        ILogger logger, string username);

    /// <summary>The caller presented no credential to check.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91242, Level = LogLevel.Information,
        Message = "A username and password are required to prove a caller by password")]
    internal static partial IGenericMessage NothingPresented(ILogger logger);
}
