using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Reference.Etl.Server.Services.JobTriggerSources.Options;

[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Event")]
public sealed class EventJobTriggerSource : JobTriggerSourceBase
{
    public EventJobTriggerSource() : base(3, "Event") { }
}
