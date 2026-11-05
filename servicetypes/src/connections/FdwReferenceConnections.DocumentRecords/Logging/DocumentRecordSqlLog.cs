using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.DocumentRecords.Logging;

/// <summary>
/// MessageLogging for the SQL Server DocumentRecord connection (FDW-3731): the statements it runs for an organization's records and what it
/// refuses. EventId range: 23300-23309.
/// </summary>
[MessageLoggingTypeCode("DOCUMENTS")]
public static partial class DocumentRecordSqlLog
{
    /// <summary>Logs a statement run for an organization.</summary>
    [MessageLogging(EventId = 23300, Level = LogLevel.Debug, Message = "Running '{statement}' for organization '{organizationId}'")]
    public static partial IGenericMessage StatementRun(ILogger logger, string statement, Guid organizationId);

    /// <summary>Logs a write that landed and the revision it raised the organization's records to.</summary>
    [MessageLogging(EventId = 23301, Level = LogLevel.Information, Message = "'{command}' landed for organization '{organizationId}' at revision {revision}")]
    public static partial IGenericMessage WriteLanded(ILogger logger, string command, Guid organizationId, long revision);

    /// <summary>Logs a write refused because the organization's records moved on since the revision it read.</summary>
    [MessageLogging(EventId = 23302, Level = LogLevel.Warning, Message = "The document records of organization '{organizationId}' are at revision {current}, not {expected}: nothing was written")]
    public static partial IGenericMessage RevisionConflict(ILogger logger, Guid organizationId, string current, string expected);

    /// <summary>Logs a read or write refused because the organization has no entity of that id.</summary>
    [MessageLogging(EventId = 23303, Level = LogLevel.Warning, Message = "Organization '{organizationId}' has no '{entity}' with the public id '{id}'")]
    public static partial IGenericMessage RecordNotFound(ILogger logger, Guid organizationId, string entity, Guid id);

    /// <summary>Logs a command this connection does not answer, or answers with another result type than it asks for.</summary>
    [MessageLogging(EventId = 23304, Level = LogLevel.Warning, Message = "The SQL Server DocumentRecord connection does not answer '{command}': {reason}")]
    public static partial IGenericMessage CommandNotAnswered(ILogger logger, string command, string reason);

    /// <summary>Logs a link target kind this connection cannot check.</summary>
    [MessageLogging(EventId = 23305, Level = LogLevel.Warning, Message = "The SQL Server DocumentRecord connection checks the link target kinds '{kinds}', not '{targetKind}'")]
    public static partial IGenericMessage TargetKindUnknown(ILogger logger, string targetKind, string kinds);
}
