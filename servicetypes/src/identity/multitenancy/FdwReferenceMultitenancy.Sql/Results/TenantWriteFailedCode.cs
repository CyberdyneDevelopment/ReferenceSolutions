using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// A write to the tenants failed in the store.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantWriteFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantWriteFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantWriteFailedCode"/> class.
    /// </summary>
    public TenantWriteFailedCode()
        : base(71002, "TenantWriteFailed",
            ResultSeverities.ByName("Error"),
            "The tenant '{TenantId}' could not be written: {Error}",
            isRetryable: false)
    {
    }
}
