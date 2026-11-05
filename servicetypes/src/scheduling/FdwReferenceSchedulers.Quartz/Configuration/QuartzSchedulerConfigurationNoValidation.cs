using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceSchedulers.Quartz.Configuration;

/// <summary>States that <see cref="QuartzSchedulerConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "QuartzSchedulerConfiguration")]
public sealed class QuartzSchedulerConfigurationNoValidation : UnvalidatedConfiguration<QuartzSchedulerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QuartzSchedulerConfigurationNoValidation"/> class.</summary>
    public QuartzSchedulerConfigurationNoValidation()
        : base("QuartzSchedulerConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
