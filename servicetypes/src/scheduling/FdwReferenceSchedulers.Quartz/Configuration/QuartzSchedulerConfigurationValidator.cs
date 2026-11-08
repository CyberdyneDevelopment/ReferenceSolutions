using FluentValidation;
using Fdw.Validation;

namespace FdwReferenceSchedulers.Quartz.Configuration;

/// <summary>
/// Validator for <see cref="QuartzSchedulerConfiguration"/> (FDW-4293): what the Quartz engine takes from the scheduler row.
/// </summary>
public sealed class QuartzSchedulerConfigurationValidator : FdwConfigurationValidator<QuartzSchedulerConfiguration>
{
    /// <summary>The reason a clustered scheduler is refused; the engine start and the validator say the same.</summary>
    public const string ClusteringRefusal =
        "EnableClustering must be false: the Quartz scheduler runs on its in-memory job store, which the reconciliation pass and the missed-fire rules depend on, and a clustered scheduler needs a persistent job store this scheduler does not have";

    /// <summary>Initializes a new instance of the <see cref="QuartzSchedulerConfigurationValidator"/> class.</summary>
    public QuartzSchedulerConfigurationValidator()
    {
        RuleFor(x => x.InstanceName)
            .NotEmpty()
            .WithMessage("InstanceName is required: it names the Quartz scheduler");

        RuleFor(x => x.ThreadCount)
            .GreaterThan(0)
            .WithMessage("ThreadCount must be greater than zero: it is the number of fires Quartz runs at once");

        RuleFor(x => x.EnableClustering)
            .Equal(false)
            .WithMessage(ClusteringRefusal);
    }
}
