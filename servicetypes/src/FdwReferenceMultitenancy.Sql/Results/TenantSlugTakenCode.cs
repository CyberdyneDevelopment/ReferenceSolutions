using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// A tenant was made with a slug another tenant has.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantSlugTaken", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantSlugTakenCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSlugTakenCode"/> class.
    /// </summary>
    public TenantSlugTakenCode()
        : base(41000, "TenantSlugTaken",
            ResultSeverities.ByName("Error"),
            "A tenant with slug '{Slug}' already exists",
            isRetryable: false)
    {
    }
}
