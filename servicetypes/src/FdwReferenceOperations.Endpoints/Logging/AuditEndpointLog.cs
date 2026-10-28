using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAudit.Endpoints.Logging;

/// <summary>MessageLogging for the administrative audit route (FDW-1586).</summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("AUDEP")]
public static partial class AuditEndpointLog
{
    /// <summary>A page of administrative changes is being read.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Listing administrative changes: page {page}, {pageSize} per page")]
    public static partial IGenericMessage Listing(ILogger logger, int page, int pageSize);

    /// <summary>A page of administrative changes was read.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Listed {count} administrative change(s)")]
    public static partial IGenericMessage Listed(ILogger logger, int count);
}
