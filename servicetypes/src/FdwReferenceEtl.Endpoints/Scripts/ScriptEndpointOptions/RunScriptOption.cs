using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The RunScript endpoint.</summary>
[TypeOption(typeof(ScriptEndpoints), "RunScript")]
public class RunScriptOption : ScriptEndpointBase<RunScriptEndpoint>
{
}
