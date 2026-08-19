using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// All tenants query failed.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "AllTenantsQueryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AllTenantsQueryFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AllTenantsQueryFailedCode"/> class.
    /// </summary>
    public AllTenantsQueryFailedCode()
        : base(71001, "AllTenantsQueryFailed",
            ResultSeverities.ByName("Error"),
            "Failed to query all tenants: {Error}",
            isRetryable: true)
    {
    }
}