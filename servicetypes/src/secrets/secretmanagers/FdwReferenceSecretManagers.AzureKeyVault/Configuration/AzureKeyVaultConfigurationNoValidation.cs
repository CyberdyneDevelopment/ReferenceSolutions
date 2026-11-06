using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.Configuration;

/// <summary>States that <see cref="AzureKeyVaultConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "AzureKeyVaultConfiguration")]
public sealed class AzureKeyVaultConfigurationNoValidation : UnvalidatedConfiguration<AzureKeyVaultConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AzureKeyVaultConfigurationNoValidation"/> class.</summary>
    public AzureKeyVaultConfigurationNoValidation()
        : base("AzureKeyVaultConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
