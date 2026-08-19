using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Tenant query by ID failed.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantQueryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantQueryFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantQueryFailedCode"/> class.
    /// </summary>
    public TenantQueryFailedCode()
        : base(70000, "TenantQueryFailed",
            ResultSeverities.ByName("Error"),
            "Failed to query tenant {TenantId}: {Error}",
            isRetryable: true)
    {
    }
}