using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// Vault authenticated the caller but its policy forbids the path.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultPermissionDenied", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultPermissionDeniedCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultPermissionDeniedCode"/> class.</summary>
    public VaultPermissionDeniedCode()
        : base(
            51001,
            "VaultPermissionDenied",
            ResultSeverities.ByName("Error"),
            "Vault policy does not permit '{path}': {error}.",
            isRetryable: false)
    { }
}
