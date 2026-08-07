using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Audit list endpoints.
/// EventId range: 8960-8969
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class AuditListLog
{
    [MessageLogging(EventId = 8960, Level = LogLevel.Information, Message = "Listing audit records with filters: entityType={entityType}, action={action}")]
    public static partial IGenericMessage ListingAuditRecords(ILogger logger, string? entityType, string? action);

    [MessageLogging(EventId = 8961, Level = LogLevel.Information, Message = "Found {count} audit records")]
    public static partial IGenericMessage AuditRecordsFound(ILogger logger, int count);

    [MessageLogging(EventId = 8962, Level = LogLevel.Error, Message = "Failed to list audit records: {message}")]
    public static partial IGenericMessage ListAuditRecordsFailed(ILogger logger, string message);
}
