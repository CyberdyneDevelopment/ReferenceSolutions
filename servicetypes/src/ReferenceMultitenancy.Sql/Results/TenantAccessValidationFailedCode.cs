using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Tenant access validation failed.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantAccessValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantAccessValidationFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAccessValidationFailedCode"/> class.
    /// </summary>
    public TenantAccessValidationFailedCode()
        : base(50001, "TenantAccessValidationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to validate tenant access for user {UserId}: {Error}",
            isRetryable: true)
    {
    }
}