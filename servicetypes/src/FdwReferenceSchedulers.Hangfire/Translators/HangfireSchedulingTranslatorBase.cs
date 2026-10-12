using Fdw.Services.Scheduling;

namespace FdwReferenceSchedulers.Hangfire.Translators;

/// <summary>
/// Base class for Hangfire scheduling command translators.
/// </summary>
public abstract class HangfireSchedulingTranslatorBase : SchedulingCommandTranslatorBase<HangfireNativeAction>
{
    /// <summary>Initializes a new instance of the <see cref="HangfireSchedulingTranslatorBase"/> class.</summary>
    /// <param name="name">Name of the translator (must match the command kind, e.g. "Create").</param>
    protected HangfireSchedulingTranslatorBase(string name) : base(name, "Hangfire")
    {
    }
}
