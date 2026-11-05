using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceSecretManagers.HashiCorpVault.Configuration;

/// <summary>States that <see cref="HashiCorpVaultConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "HashiCorpVaultConfiguration")]
public sealed class HashiCorpVaultConfigurationNoValidation : UnvalidatedConfiguration<HashiCorpVaultConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HashiCorpVaultConfigurationNoValidation"/> class.</summary>
    public HashiCorpVaultConfigurationNoValidation()
        : base("HashiCorpVaultConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
