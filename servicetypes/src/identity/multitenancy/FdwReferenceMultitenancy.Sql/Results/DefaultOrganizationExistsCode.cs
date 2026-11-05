using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// The tenant already has a default organization, so a second one is refused (FDW-3668).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "DefaultOrganizationExists", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DefaultOrganizationExistsCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultOrganizationExistsCode"/> class.
    /// </summary>
    public DefaultOrganizationExistsCode()
        : base(41012, "DefaultOrganizationExists",
            ResultSeverities.ByName("Error"),
            "Tenant '{TenantId}' already has a default organization; a tenant has exactly one",
            isRetryable: false)
    {
    }
}
