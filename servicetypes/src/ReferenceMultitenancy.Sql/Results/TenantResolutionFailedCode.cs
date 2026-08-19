using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Tenant resolution failed.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantResolutionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantResolutionFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionFailedCode"/> class.
    /// </summary>
    public TenantResolutionFailedCode()
        : base(31000, "TenantResolutionFailed",
            ResultSeverities.ByName("Warning"),
            "Could not resolve tenant from request context",
            isRetryable: false)
    {
    }
}