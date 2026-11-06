using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.WebMcp.Configuration;

/// <summary>States that <see cref="WebMcpHostConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "WebMcpHostConfiguration")]
public sealed class WebMcpHostConfigurationNoValidation : UnvalidatedConfiguration<WebMcpHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WebMcpHostConfigurationNoValidation"/> class.</summary>
    public WebMcpHostConfigurationNoValidation()
        : base("WebMcpHostConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
