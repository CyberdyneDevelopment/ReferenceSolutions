using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// A command this secret manager does not implement was submitted.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultOperationNotSupported", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultOperationNotSupportedCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultOperationNotSupportedCode"/> class.</summary>
    public VaultOperationNotSupportedCode()
        : base(
            61002,
            "VaultOperationNotSupported",
            ResultSeverities.ByName("Error"),
            "The Vault secret manager does not implement '{operation}'.",
            isRetryable: false)
    { }
}
