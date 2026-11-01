using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// Vault could not be reached.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultUnreachable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultUnreachableCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultUnreachableCode"/> class.</summary>
    public VaultUnreachableCode()
        : base(
            71000,
            "VaultUnreachable",
            ResultSeverities.ByName("Error"),
            "Could not reach Vault at '{address}'.",
            isRetryable: true)
    { }
}
