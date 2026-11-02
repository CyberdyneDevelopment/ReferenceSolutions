using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// A slug is already used (FDW-1758).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "OrganizationSlugTaken", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OrganizationSlugTakenCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationSlugTakenCode"/> class.
    /// </summary>
    public OrganizationSlugTakenCode()
        : base(41002, "OrganizationSlugTaken",
            ResultSeverities.ByName("Error"),
            "An organization with slug '{Slug}' already exists in the tenant",
            isRetryable: false)
    {
    }
}
