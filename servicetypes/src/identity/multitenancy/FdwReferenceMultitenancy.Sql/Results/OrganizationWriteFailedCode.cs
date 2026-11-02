using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Results;

/// <summary>
/// The organization could not be written (FDW-1758).
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "OrganizationWriteFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OrganizationWriteFailedCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationWriteFailedCode"/> class.
    /// </summary>
    public OrganizationWriteFailedCode()
        : base(71003, "OrganizationWriteFailed",
            ResultSeverities.ByName("Error"),
            "The organization '{OrganizationId}' could not be written: {Error}",
            isRetryable: false)
    {
    }
}
