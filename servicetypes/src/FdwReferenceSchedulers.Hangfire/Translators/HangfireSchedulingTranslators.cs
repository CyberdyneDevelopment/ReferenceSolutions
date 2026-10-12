using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Scheduling.Abstractions;

namespace FdwReferenceSchedulers.Hangfire.Translators;

/// <summary>
/// TypeCollection of Hangfire scheduling command translators. One command per operation
/// (Create/Update/Delete/Pause/Resume/Trigger), shared with every other scheduler implementation —
/// only the translator, collected here, is Hangfire-specific. Mirrors <c>MsSqlDataCommandTranslators</c>.
/// </summary>
[TypeCollection(typeof(HangfireSchedulingTranslatorBase), typeof(ISchedulingCommandTranslator<HangfireNativeAction>), typeof(HangfireSchedulingTranslators))]
[ExcludeFromCodeCoverage]
public abstract partial class HangfireSchedulingTranslators :
    TypeCollectionBase<HangfireSchedulingTranslatorBase, ISchedulingCommandTranslator<HangfireNativeAction>>
{
    // Source generator creates static properties Create, Update, Delete, Pause, Resume, Trigger.
}
