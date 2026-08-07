using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Reference.Etl.Server.Services.JobTriggerSources;

[ExcludeFromCodeCoverage]
public abstract class JobTriggerSourceBase : TypeOptionBase<int, JobTriggerSourceBase>, IJobTriggerSource
{
    protected JobTriggerSourceBase(int id, string name) : base(id, name) { }
}
