using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Reference.Etl.Server.Services.JobTriggerSources.Options;

[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Api")]
public sealed class ApiJobTriggerSource : JobTriggerSourceBase
{
    public ApiJobTriggerSource() : base(4, "Api") { }
}
