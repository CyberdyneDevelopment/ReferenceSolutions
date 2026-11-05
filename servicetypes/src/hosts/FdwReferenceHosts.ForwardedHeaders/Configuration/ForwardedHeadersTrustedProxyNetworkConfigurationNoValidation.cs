using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.ForwardedHeaders.Configuration;

/// <summary>States that <see cref="ForwardedHeadersTrustedProxyNetwork"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ForwardedHeadersTrustedProxyNetwork")]
public sealed class ForwardedHeadersTrustedProxyNetworkConfigurationNoValidation : UnvalidatedConfiguration<ForwardedHeadersTrustedProxyNetwork>
{
    /// <summary>Initializes a new instance of the <see cref="ForwardedHeadersTrustedProxyNetworkConfigurationNoValidation"/> class.</summary>
    public ForwardedHeadersTrustedProxyNetworkConfigurationNoValidation()
        : base("ForwardedHeadersTrustedProxyNetwork", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
