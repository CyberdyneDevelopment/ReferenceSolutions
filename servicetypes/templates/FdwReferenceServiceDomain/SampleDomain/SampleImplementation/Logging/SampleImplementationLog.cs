using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace SampleDomain.SampleImplementation.Logging;

/// <summary>
/// MessageLogging for the SampleImplementation option's registration lifecycle.
/// </summary>
/// <remarks>EventId range: EVENTID_BASE + 10 onwards — see SampleDomainLog's remarks for the split.</remarks>
[MessageLoggingTypeCode("SVCDOM")]
internal static partial class SampleImplementationLog
{
    /// <summary>This option registered its own configuration provider into the domain provider.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="optionName">This option's own name — the Implementation it registered under.</param>
    // Information: the milestone a reader at default verbosity needs, matching MsSqlConnectionType's
    // own ConfigurationProviderRegistered line.
    [MessageLogging(EventId = EVENTID_BASE + 10, Level = LogLevel.Information,
        Message = "SampleImplementation registered its configuration provider under '{optionName}'")]
    internal static partial IGenericMessage ConfigurationProviderRegistered(ILogger logger, string optionName);
}
