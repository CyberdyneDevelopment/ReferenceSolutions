using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Tenant slug is required but not provided.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantSlugRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantSlugRequiredCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSlugRequiredCode"/> class.
    /// </summary>
    public TenantSlugRequiredCode()
        : base(20000, "TenantSlugRequired",
            ResultSeverities.ByName("Warning"),
            "Tenant slug is required",
            isRetryable: false)
    {
    }
}