using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// A required Vault configuration value has no value; the manager cannot be built.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultConfigurationValueMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultConfigurationValueMissingCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultConfigurationValueMissingCode"/> class.</summary>
    public VaultConfigurationValueMissingCode()
        : base(
            61000,
            "VaultConfigurationValueMissing",
            ResultSeverities.ByName("Critical"),
            "Vault configuration '{configurationName}' is missing required value '{property}'.",
            isRetryable: false)
    { }
}
