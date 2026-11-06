using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceAuthentication.ForeignToken;

/// <summary>States that <see cref="ForeignAuthorityConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ForeignAuthorityConfiguration")]
public sealed class ForeignAuthorityConfigurationNoValidation : UnvalidatedConfiguration<ForeignAuthorityConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ForeignAuthorityConfigurationNoValidation"/> class.</summary>
    public ForeignAuthorityConfigurationNoValidation()
        : base("ForeignAuthorityConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
