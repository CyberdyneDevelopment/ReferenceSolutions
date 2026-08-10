using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Ui.Logging;

/// <summary>
/// MessageLogging class for application startup operations.
/// EventId range: 9600-9609
/// </summary>
public static partial class ProgramLog
{
    [MessageLogging(
        EventId = 9600,
        Level = LogLevel.Critical,
        Message = "Required configuration 'ApiEndpoints:Api' is missing. " +
            "Set this value in appsettings.[environment].json or via environment variable. " +
            "Application cannot start without a valid API base URL")]
    public static partial IGenericMessage ApiBaseUrlMissing(ILogger logger);
}
