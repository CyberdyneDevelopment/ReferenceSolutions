using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Tenant not found by slug.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantSlugNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantSlugNotFoundCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSlugNotFoundCode"/> class.
    /// </summary>
    public TenantSlugNotFoundCode()
        : base(31001, "TenantSlugNotFound",
            ResultSeverities.ByName("Warning"),
            "Tenant with slug '{Slug}' not found",
            isRetryable: false)
    {
    }
}