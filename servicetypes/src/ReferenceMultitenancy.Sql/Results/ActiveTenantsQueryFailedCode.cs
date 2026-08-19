using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Active tenants query failed.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "ActiveTenantsQueryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ActiveTenantsQueryFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveTenantsQueryFailedCode"/> class.
    /// </summary>
    public ActiveTenantsQueryFailedCode()
        : base(70001, "ActiveTenantsQueryFailed",
            ResultSeverities.ByName("Error"),
            "Failed to query active tenants: {Error}",
            isRetryable: true)
    {
    }
}