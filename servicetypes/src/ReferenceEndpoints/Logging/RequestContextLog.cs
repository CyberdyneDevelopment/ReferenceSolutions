using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceEndpoints.Logging;

/// <summary>
/// Source-generated logging methods for RequestContextMiddleware.
/// EventId range: 549
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("HOSTING")]
public static partial class RequestContextLog
{
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace, Message = "Request context built for tenant {tenantId} with {roleCount} roles")]
    public static partial IGenericMessage RequestContextBuilt(ILogger logger, string tenantId, int roleCount);
}
