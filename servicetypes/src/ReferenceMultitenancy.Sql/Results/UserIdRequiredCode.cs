using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace ReferenceMultitenancy.Sql.Results;

/// <summary>
/// User ID is required but not provided.
/// </summary>
[TypeOption(typeof(SqlTenantResultCodes), "UserIdRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UserIdRequiredCode : SqlTenantResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserIdRequiredCode"/> class.
    /// </summary>
    public UserIdRequiredCode()
        : base(21000, "UserIdRequired",
            ResultSeverities.ByName("Warning"),
            "User ID is required",
            isRetryable: false)
    {
    }
}