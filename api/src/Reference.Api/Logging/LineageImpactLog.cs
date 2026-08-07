using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Lineage impact endpoints.
/// EventId range: 8950-8959
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class LineageImpactLog
{
    [MessageLogging(EventId = 8950, Level = LogLevel.Information, Message = "Getting lineage impact for entity '{entityName}'")]
    public static partial IGenericMessage GettingImpact(ILogger logger, string entityName);

    [MessageLogging(EventId = 8951, Level = LogLevel.Information, Message = "Found {count} impacted DataSets for entity '{entityName}'")]
    public static partial IGenericMessage ImpactFound(ILogger logger, int count, string entityName);
}
