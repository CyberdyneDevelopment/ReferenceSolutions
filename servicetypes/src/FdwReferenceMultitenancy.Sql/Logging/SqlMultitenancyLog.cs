using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace FdwReferenceMultitenancy.Sql.Logging;

/// <summary>
/// MessageLogging for the SQL-backed multitenancy option's own configuration resolution
/// (<c>settings.SqlTenantProviderConfiguration</c>).
/// </summary>
[MessageLoggingTypeCode("SQLTENANT")]
public static partial class SqlMultitenancyLog
{
    /// <summary>
    /// Logs when <c>settings.SqlTenantProviderConfiguration</c> has no current row. The "Sql" Multitenancy option
    /// was explicitly selected (or derived from the schema's declared tenant containers), so a missing
    /// tenant-provider row is a startup misconfiguration, not a silent single-tenant fallback
    /// (NO FALLBACKS).
    /// </summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Critical,
        Message = "[Multitenancy] Multitenancy Implementation 'Sql' is configured but settings.SqlTenantProviderConfiguration has no current row — add a SqlTenantProvider row or select a different Multitenancy Implementation")]
    public static partial IGenericMessage TenantProviderRowMissing(
        ILogger logger);

    /// <summary>
    /// Logs when the <c>settings.SqlTenantProviderConfiguration</c> read itself failed — the gateway could not
    /// reach the container at all (connection, undeclared container, permissions).
    /// </summary>
    /// <remarks>
    /// Why this is separate from <see cref="TenantProviderRowMissing"/>: a failed read and an empty
    /// table are different faults with different fixes, and the row-missing message tells the operator
    /// to "add a SqlTenantProvider row" — advice that is actively wrong when the read never completed.
    /// The reason from the failing result is carried through so the actual cause is in the log line.
    /// </remarks>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Critical,
        Message = "[Multitenancy] Multitenancy Implementation 'Sql' is configured but the settings.SqlTenantProviderConfiguration read failed: {reason}")]
    public static partial IGenericMessage TenantProviderReadFailed(
        ILogger logger,
        string reason);
}
