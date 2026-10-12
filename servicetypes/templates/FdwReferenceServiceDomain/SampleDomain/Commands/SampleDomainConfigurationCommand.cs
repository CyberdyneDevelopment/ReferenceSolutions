using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Services.Configuration;

namespace SampleDomain.Commands;

/// <summary>ConfigurationCommands TypeOption for the SampleDomain domain table.</summary>
/// <remarks>
/// A domain declares no record of its own. Its rows are the platform's one
/// <see cref="DomainConfiguration"/>, and this names the table they live in for this domain.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "SAMPLE_CATEGORY")]
public sealed class SampleDomainConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public SampleDomainConfigurationCommand() : base("SAMPLE_CATEGORY") { }
}
