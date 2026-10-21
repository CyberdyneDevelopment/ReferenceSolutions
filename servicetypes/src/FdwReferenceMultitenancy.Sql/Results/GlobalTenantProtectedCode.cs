using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// The global tenant was disabled or deleted.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "GlobalTenantProtected", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GlobalTenantProtectedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalTenantProtectedCode"/> class.
    /// </summary>
    public GlobalTenantProtectedCode()
        : base(41001, "GlobalTenantProtected",
            ResultSeverities.ByName("Error"),
            "Tenant '{TenantId}' is the global tenant, which the platform runs in; it cannot be disabled or deleted",
            isRetryable: false)
    {
    }
}
