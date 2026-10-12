using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace SampleStep;

/// <summary>
/// MessageLogging for this step.
/// </summary>
/// <remarks>
/// EventId range: EVENTID_BASE onwards. Never log a credential, a token, or a subject identifier
/// from an external authority — a log reaches whoever can read logs, and those three identify or
/// impersonate a person. An issuer name and a local principal id correlate as well and grant nothing.
/// <para>
/// No <c>NotInitialized</c> here — <see cref="Fdw.Services.Authentication.Flow.AuthenticationStepTypeBase{TService,TFactory}.NotInitialized"/>
/// already reports that failure with the one code every step shares, so a step never declares its
/// own copy.
/// </para>
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class SampleStepLog
{
    /// <summary>The step ran.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="example">The configured setting, for correlation.</param>
    // Trace is the right level for the ordinary path: a login that works should cost nothing to log.
    [MessageLogging(EventId = EVENTID_BASE, Level = LogLevel.Trace,
        Message = "SampleStep ran with {example}")]
    internal static partial IGenericMessage Executed(ILogger logger, string example);

    /// <summary>The step could not do its job.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">Why.</param>
    // Warning, not Error, when the refusal is the step working — a bad credential, an expired
    // assertion. Error is for a defect: something misconfigured, or a contract broken.
    [MessageLogging(EventId = EVENTID_BASE + 1, Level = LogLevel.Warning,
        Message = "SampleStep refused: {reason}")]
    internal static partial IGenericMessage Refused(ILogger logger, string reason);
}
