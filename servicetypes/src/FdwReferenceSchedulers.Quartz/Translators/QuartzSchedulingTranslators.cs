using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Scheduling.Abstractions;

namespace FdwReferenceSchedulers.Quartz.Translators;

/// <summary>
/// TypeCollection of Quartz scheduling command translators. One command per operation
/// (Create/Update/Delete/Pause/Resume/Trigger), shared with every other scheduler implementation —
/// only the translator, collected here, is Quartz-specific. Mirrors <c>MsSqlDataCommandTranslators</c>.
/// </summary>
[TypeCollection(typeof(QuartzSchedulingTranslatorBase), typeof(ISchedulingCommandTranslator<QuartzNativeAction>), typeof(QuartzSchedulingTranslators))]
[ExcludeFromCodeCoverage]
public abstract partial class QuartzSchedulingTranslators :
    TypeCollectionBase<QuartzSchedulingTranslatorBase, ISchedulingCommandTranslator<QuartzNativeAction>>
{
    // Source generator creates static properties Create, Update, Delete, Pause, Resume, Trigger.
}
