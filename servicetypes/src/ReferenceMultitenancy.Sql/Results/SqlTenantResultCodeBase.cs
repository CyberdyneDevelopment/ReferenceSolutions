using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Base class for SQL Tenant result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SqlTenantResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected SqlTenantResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTenantResultCodeBase"/> class.
    /// </summary>
    protected SqlTenantResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "SqlTenant", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/>
    /// (Id == EventId == number, Code == "SQLTENANT-{number}").
    /// </summary>
    protected SqlTenantResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SQLTENANT", isRetryable)
    {
    }
}