using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Sample;

/// <summary>The messages this mechanism reports.</summary>
/// <remarks>
/// Failures carry a declared message, never a bare string: a result built from a string has no
/// event id, no structured fields and nothing to grep for when the same failure shows up in a log
/// six months later. Pick event ids from a range this domain owns.
/// </remarks>
public static partial class SampleAuthenticationLog
{
    /// <summary>Reports an entry that named no authority.</summary>
    [MessageLogging(
        EventId = 90001,
        Level = LogLevel.Error,
        Message = "AuthenticationServices entry '{serviceName}' has no Authority, so no scheme was taken for it")]
    public static partial IGenericMessage AuthorityMissing(ILogger logger, string serviceName);

    /// <summary>Reports an entry that carried no name.</summary>
    /// <remarks>
    /// Reported without one, because the missing value IS the name — there is nothing to correlate
    /// this entry by, which is the whole reason it cannot be given a scheme.
    /// </remarks>
    [MessageLogging(
        EventId = 90002,
        Level = LogLevel.Error,
        Message = "An AuthenticationServices entry has no Name, so no scheme was taken for it")]
    public static partial IGenericMessage EntryMissingName(ILogger logger);
}
