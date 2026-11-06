using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.EmptyBody.Configuration;

/// <summary>States that <see cref="EmptyBodyRoute"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "EmptyBodyRoute")]
public sealed class EmptyBodyRouteConfigurationNoValidation : UnvalidatedConfiguration<EmptyBodyRoute>
{
    /// <summary>Initializes a new instance of the <see cref="EmptyBodyRouteConfigurationNoValidation"/> class.</summary>
    public EmptyBodyRouteConfigurationNoValidation()
        : base("EmptyBodyRoute", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
