using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Management.UI.Tailwind.Logging;

/// <summary>
/// Structured logging for sidebar navigation resolution.
/// EventId range: 9820-9829
/// </summary>
public static partial class NavLog
{
    /// <summary>Logs that a page declaring a sidebar entry has no route the entry can link to.</summary>
    [MessageLogging(EventId = 9820, Level = LogLevel.Error,
        Message = "Page '{pageName}' ({componentName}) declares a sidebar entry but its component has no parameterless route")]
    public static partial IGenericMessage NoParameterlessRoute(ILogger logger, string pageName, string componentName);
}
