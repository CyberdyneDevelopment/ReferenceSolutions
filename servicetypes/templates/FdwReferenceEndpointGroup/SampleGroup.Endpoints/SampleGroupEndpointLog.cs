using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace SampleGroup.Endpoints;

/// <summary>The messages the SampleGroup endpoint reports.</summary>
/// <remarks>
/// A failure carries a declared message, never a bare string, so it has an event id and structured fields to find later. The event id here is
/// the template's: pick the next free one in the range your package owns.
/// </remarks>
public static partial class SampleGroupEndpointLog
{
    /// <summary>Reports a member of the endpoint that has not been connected to the service that owns the data.</summary>
    [MessageLogging(
        EventId = 90001,
        Level = LogLevel.Error,
        Message = "The SampleGroup endpoint's '{member}' is not connected to the service that owns the data")]
    public static partial IGenericMessage NotWired(ILogger logger, string member);
}
