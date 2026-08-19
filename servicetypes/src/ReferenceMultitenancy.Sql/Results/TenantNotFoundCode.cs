using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Tenant not found by ID.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantNotFoundCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotFoundCode"/> class.
    /// </summary>
    public TenantNotFoundCode()
        : base(30000, "TenantNotFound",
            ResultSeverities.ByName("Warning"),
            "Tenant with ID '{TenantId}' not found",
            isRetryable: false)
    {
    }
}