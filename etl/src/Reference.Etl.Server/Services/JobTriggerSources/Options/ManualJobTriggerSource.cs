using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Reference.Etl.Server.Services.JobTriggerSources.Options;

[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Manual")]
public sealed class ManualJobTriggerSource : JobTriggerSourceBase
{
    public ManualJobTriggerSource() : base(1, "Manual") { }
}
