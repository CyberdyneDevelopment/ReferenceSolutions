using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// TypeCollection for SQL Tenant result codes.
/// Codes use the categorized-number scheme (Id == EventId == number, Code == "SQLTENANT-{number}").
/// </summary>
[TypeCollection(typeof(SqlTenantResultCodeBase), typeof(IResultCode), typeof(SqlTenantResultCodes))]
public abstract partial class SqlTenantResultCodes : TypeCollectionBase<SqlTenantResultCodeBase, IResultCode>
{
}