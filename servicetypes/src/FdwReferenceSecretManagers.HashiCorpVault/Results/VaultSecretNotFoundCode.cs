using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// The requested Vault path holds no secret.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultSecretNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultSecretNotFoundCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultSecretNotFoundCode"/> class.</summary>
    public VaultSecretNotFoundCode()
        : base(
            31000,
            "VaultSecretNotFound",
            ResultSeverities.ByName("Error"),
            "Vault has nothing at '{path}'.",
            isRetryable: false)
    { }
}
