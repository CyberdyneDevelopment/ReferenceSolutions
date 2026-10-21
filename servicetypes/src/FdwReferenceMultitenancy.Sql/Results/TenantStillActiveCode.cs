using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// A tenant that is still active was asked to be deleted (FDW-1759).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantStillActive", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantStillActiveCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantStillActiveCode"/> class.
    /// </summary>
    public TenantStillActiveCode()
        : base(41004, "TenantStillActive",
            ResultSeverities.ByName("Error"),
            "Tenant '{TenantId}' is still active; disable it before deleting it",
            isRetryable: false)
    {
    }
}
