using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// Invalid user ID format.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "InvalidUserIdFormat", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidUserIdFormatCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidUserIdFormatCode"/> class.
    /// </summary>
    public InvalidUserIdFormatCode()
        : base(20001, "InvalidUserIdFormat",
            ResultSeverities.ByName("Warning"),
            "Invalid user ID format: {UserId}",
            isRetryable: false)
    {
    }
}