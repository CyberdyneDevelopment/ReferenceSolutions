using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// An organization needs a slug (FDW-1758).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "OrganizationSlugRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OrganizationSlugRequiredCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationSlugRequiredCode"/> class.
    /// </summary>
    public OrganizationSlugRequiredCode()
        : base(20004, "OrganizationSlugRequired",
            ResultSeverities.ByName("Error"),
            "An organization must have a slug",
            isRetryable: false)
    {
    }
}
