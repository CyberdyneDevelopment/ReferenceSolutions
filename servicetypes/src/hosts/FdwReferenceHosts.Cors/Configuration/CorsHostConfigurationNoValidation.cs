using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.Cors.Configuration;

/// <summary>States that <see cref="CorsHostConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "CorsHostConfiguration")]
public sealed class CorsHostConfigurationNoValidation : UnvalidatedConfiguration<CorsHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CorsHostConfigurationNoValidation"/> class.</summary>
    public CorsHostConfigurationNoValidation()
        : base("CorsHostConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
