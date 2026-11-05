using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceAuthentication.OidcRedirect;

/// <summary>States that <see cref="OidcAuthorityConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "OidcAuthorityConfiguration")]
public sealed class OidcAuthorityConfigurationNoValidation : UnvalidatedConfiguration<OidcAuthorityConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OidcAuthorityConfigurationNoValidation"/> class.</summary>
    public OidcAuthorityConfigurationNoValidation()
        : base("OidcAuthorityConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
