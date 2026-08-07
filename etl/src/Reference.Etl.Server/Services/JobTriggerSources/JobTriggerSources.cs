using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Reference.Etl.Server.Services.JobTriggerSources;

[ExcludeFromCodeCoverage]
[TypeCollection(typeof(JobTriggerSourceBase), typeof(IJobTriggerSource), typeof(JobTriggerSourceTypes))]
public abstract partial class JobTriggerSourceTypes : TypeCollectionBase<JobTriggerSourceBase, IJobTriggerSource>
{
}
