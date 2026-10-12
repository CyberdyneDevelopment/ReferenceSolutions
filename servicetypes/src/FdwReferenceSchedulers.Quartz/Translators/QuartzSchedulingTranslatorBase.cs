using Fdw.Services.Scheduling;

namespace FdwReferenceSchedulers.Quartz.Translators;

/// <summary>
/// Base class for Quartz scheduling command translators.
/// </summary>
public abstract class QuartzSchedulingTranslatorBase : SchedulingCommandTranslatorBase<QuartzNativeAction>
{
    /// <summary>Initializes a new instance of the <see cref="QuartzSchedulingTranslatorBase"/> class.</summary>
    /// <param name="name">Name of the translator (must match the command kind, e.g. "Create").</param>
    protected QuartzSchedulingTranslatorBase(string name) : base(name, "Quartz")
    {
    }
}
