using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceSchedulers.Quartz.Configuration;

/// <summary>States that <see cref="QuartzSchedulerConfiguration"/> is checked by <see cref="QuartzSchedulerConfigurationValidator"/> when it is written (FDW-4293, replacing the FDW-3541 statement that it had none).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "QuartzSchedulerConfiguration")]
public sealed class QuartzSchedulerConfigurationValidation : ValidatedConfiguration<QuartzSchedulerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QuartzSchedulerConfigurationValidation"/> class.</summary>
    public QuartzSchedulerConfigurationValidation()
        : base("QuartzSchedulerConfiguration", new QuartzSchedulerConfigurationValidator())
    {
    }
}
