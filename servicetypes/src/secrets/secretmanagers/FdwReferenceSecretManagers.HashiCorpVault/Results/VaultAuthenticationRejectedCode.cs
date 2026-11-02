using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// Vault refused this process's login.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultAuthenticationRejected", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultAuthenticationRejectedCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultAuthenticationRejectedCode"/> class.</summary>
    public VaultAuthenticationRejectedCode()
        : base(
            51000,
            "VaultAuthenticationRejected",
            ResultSeverities.ByName("Error"),
            "Vault rejected this process's credential via {authMethod}: {error}.",
            isRetryable: false)
    { }
}
