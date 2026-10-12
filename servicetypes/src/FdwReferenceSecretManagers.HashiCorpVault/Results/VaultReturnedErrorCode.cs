using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// Vault answered with an unexpected status.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultReturnedError", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultReturnedErrorCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultReturnedErrorCode"/> class.</summary>
    public VaultReturnedErrorCode()
        : base(
            71001,
            "VaultReturnedError",
            ResultSeverities.ByName("Error"),
            "Vault at '{address}' returned {statusCode}.",
            isRetryable: true)
    { }
}
