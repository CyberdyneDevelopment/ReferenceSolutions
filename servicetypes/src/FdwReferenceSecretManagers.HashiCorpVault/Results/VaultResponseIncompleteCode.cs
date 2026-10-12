using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// A Vault response was well-formed but missing a required field.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultResponseIncomplete", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultResponseIncompleteCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultResponseIncompleteCode"/> class.</summary>
    public VaultResponseIncompleteCode()
        : base(
            91001,
            "VaultResponseIncomplete",
            ResultSeverities.ByName("Error"),
            "Vault response ({context}) carried no '{field}'.",
            isRetryable: false)
    { }
}
