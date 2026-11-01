using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using System;

namespace FdwReferenceMultitenancy.Sql.Logging;

/// <summary>
/// MessageLogging for the writes to tenants (FDW-1578).
/// EventId range: 12800-12802 (made, changed, removed).
/// </summary>
[MessageLoggingTypeCode("SQLTENANT")]
public static partial class SqlTenantWriterLog
{
    /// <summary>Logs that a tenant was made.</summary>
    [MessageLogging(EventId = 12800, Level = LogLevel.Information, Message = "Tenant '{tenantId}' ('{slug}') was made")]
    public static partial IGenericMessage TenantMade(ILogger logger, Guid tenantId, string slug);

    /// <summary>Logs that a tenant was changed.</summary>
    [MessageLogging(EventId = 12801, Level = LogLevel.Information, Message = "Tenant '{tenantId}' was changed")]
    public static partial IGenericMessage TenantChanged(ILogger logger, Guid tenantId);

    /// <summary>Logs that a tenant was removed.</summary>
    [MessageLogging(EventId = 12802, Level = LogLevel.Information, Message = "Tenant '{tenantId}' was removed")]
    public static partial IGenericMessage TenantRemoved(ILogger logger, Guid tenantId);
}
