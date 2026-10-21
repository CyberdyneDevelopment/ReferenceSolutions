using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// An organization needs a name (FDW-1758).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "OrganizationNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OrganizationNameRequiredCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationNameRequiredCode"/> class.
    /// </summary>
    public OrganizationNameRequiredCode()
        : base(20003, "OrganizationNameRequired",
            ResultSeverities.ByName("Error"),
            "An organization must have a name",
            isRetryable: false)
    {
    }
}
