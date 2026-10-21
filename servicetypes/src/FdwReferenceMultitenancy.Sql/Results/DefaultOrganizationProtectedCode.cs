using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// The default organization is protected (FDW-1758).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "DefaultOrganizationProtected", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DefaultOrganizationProtectedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultOrganizationProtectedCode"/> class.
    /// </summary>
    public DefaultOrganizationProtectedCode()
        : base(41003, "DefaultOrganizationProtected",
            ResultSeverities.ByName("Error"),
            "Organization '{OrganizationId}' is the default organization of its tenant, which the tenant's people act in; it cannot be disabled or removed",
            isRetryable: false)
    {
    }
}
