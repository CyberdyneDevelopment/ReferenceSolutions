using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// A tenant was being made but no default (global) tenant exists to take its palette from (FDW-3925).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "DefaultTenantNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DefaultTenantNotFoundCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTenantNotFoundCode"/> class.
    /// </summary>
    public DefaultTenantNotFoundCode()
        : base(30002, "DefaultTenantNotFound",
            ResultSeverities.ByName("Error"),
            "No default (global) tenant is configured, so a new tenant has no palette to take what it leaves out from",
            isRetryable: false)
    {
    }
}
