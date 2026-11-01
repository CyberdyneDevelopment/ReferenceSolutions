using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// A Vault error body was not the JSON its API contract calls for.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultErrorResponseUnparseable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultErrorResponseUnparseableCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultErrorResponseUnparseableCode"/> class.</summary>
    public VaultErrorResponseUnparseableCode()
        : base(
            71002,
            "VaultErrorResponseUnparseable",
            ResultSeverities.ByName("Error"),
            "Vault error response was not JSON — the request may not have reached Vault's API.",
            isRetryable: false)
    { }
}
