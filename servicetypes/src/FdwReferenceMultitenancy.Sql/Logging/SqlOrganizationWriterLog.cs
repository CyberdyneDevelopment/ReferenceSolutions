using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMultitenancy.Sql.Logging;

/// <summary>
/// MessageLogging for the writes to organizations (FDW-1758).
/// EventId range: 12803-12805 (made, changed, removed).
/// </summary>
[MessageLoggingTypeCode("SQLTENANT")]
public static partial class SqlOrganizationWriterLog
{
    /// <summary>Logs that an organization was made.</summary>
    [MessageLogging(EventId = 12803, Level = LogLevel.Information, Message = "Organization '{organizationId}' ('{slug}') was made in tenant '{tenantId}'")]
    public static partial IGenericMessage OrganizationMade(ILogger logger, Guid organizationId, string slug, Guid tenantId);

    /// <summary>Logs that an organization was changed.</summary>
    [MessageLogging(EventId = 12804, Level = LogLevel.Information, Message = "Organization '{organizationId}' was changed")]
    public static partial IGenericMessage OrganizationChanged(ILogger logger, Guid organizationId);

    /// <summary>Logs that an organization was removed.</summary>
    [MessageLogging(EventId = 12805, Level = LogLevel.Information, Message = "Organization '{organizationId}' was removed")]
    public static partial IGenericMessage OrganizationRemoved(ILogger logger, Guid organizationId);
}
