using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSamples.Endpoints.Logging;

/// <summary>
/// MessageLogging class for the demonstration endpoints.
/// EventId range: 10400-10449
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class SampleLog
{
    // Why Debug: this fires on every authenticated request to the demonstration endpoint; it is a
    // per-request breadcrumb, not a milestone. Mirrors AuthenticationLog.ProtectedResourceAccessed,
    // which this replaces so the samples package need not reference the auth domain.
    [MessageLogging(
        EventId = 10400,
        Level = LogLevel.Debug,
        Message = "Protected resource accessed by user '{username}'")]
    public static partial IGenericMessage ProtectedResourceAccessed(ILogger logger, string username);
}
