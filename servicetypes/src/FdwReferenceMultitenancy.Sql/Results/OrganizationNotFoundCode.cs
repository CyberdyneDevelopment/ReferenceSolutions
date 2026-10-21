using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// No such organization (FDW-1758).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "OrganizationNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OrganizationNotFoundCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationNotFoundCode"/> class.
    /// </summary>
    public OrganizationNotFoundCode()
        : base(30001, "OrganizationNotFound",
            ResultSeverities.ByName("Error"),
            "There is no organization '{OrganizationId}'",
            isRetryable: false)
    {
    }
}
