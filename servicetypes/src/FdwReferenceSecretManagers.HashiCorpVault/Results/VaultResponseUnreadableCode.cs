using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// A Vault response was not parseable JSON.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultResponseUnreadable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultResponseUnreadableCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultResponseUnreadableCode"/> class.</summary>
    public VaultResponseUnreadableCode()
        : base(
            91000,
            "VaultResponseUnreadable",
            ResultSeverities.ByName("Error"),
            "Could not read Vault's response from '{address}'.",
            isRetryable: false)
    { }
}
