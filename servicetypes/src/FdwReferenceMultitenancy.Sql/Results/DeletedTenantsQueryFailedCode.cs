using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// Deleted tenants query failed.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "DeletedTenantsQueryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeletedTenantsQueryFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeletedTenantsQueryFailedCode"/> class.
    /// </summary>
    public DeletedTenantsQueryFailedCode()
        : base(71004, "DeletedTenantsQueryFailed",
            ResultSeverities.ByName("Error"),
            "Failed to query deleted tenants: {Error}",
            isRetryable: true)
    {
    }
}