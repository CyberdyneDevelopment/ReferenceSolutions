using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceLogging.Serilog;

/// <summary>States that <see cref="SerilogLevelOverride"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SerilogLevelOverride")]
public sealed class SerilogLevelOverrideConfigurationNoValidation : UnvalidatedConfiguration<SerilogLevelOverride>
{
    /// <summary>Initializes a new instance of the <see cref="SerilogLevelOverrideConfigurationNoValidation"/> class.</summary>
    public SerilogLevelOverrideConfigurationNoValidation()
        : base("SerilogLevelOverride", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
