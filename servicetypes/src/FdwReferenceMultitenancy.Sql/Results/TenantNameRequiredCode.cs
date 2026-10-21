using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// A tenant was made or renamed with no name.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "TenantNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantNameRequiredCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNameRequiredCode"/> class.
    /// </summary>
    public TenantNameRequiredCode()
        : base(20002, "TenantNameRequired",
            ResultSeverities.ByName("Error"),
            "A tenant needs a name",
            isRetryable: false)
    {
    }
}
