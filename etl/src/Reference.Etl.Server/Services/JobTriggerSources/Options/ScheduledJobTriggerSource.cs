using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Reference.Etl.Server.Services.JobTriggerSources.Options;

[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Scheduled")]
public sealed class ScheduledJobTriggerSource : JobTriggerSourceBase
{
    public ScheduledJobTriggerSource() : base(2, "Scheduled") { }
}
