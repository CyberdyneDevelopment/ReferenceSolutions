using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Tenant query by slug failed.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantSlugQueryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantSlugQueryFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSlugQueryFailedCode"/> class.
    /// </summary>
    public TenantSlugQueryFailedCode()
        : base(71000, "TenantSlugQueryFailed",
            ResultSeverities.ByName("Error"),
            "Failed to query tenant by slug '{Slug}': {Error}",
            isRetryable: true)
    {
    }
}