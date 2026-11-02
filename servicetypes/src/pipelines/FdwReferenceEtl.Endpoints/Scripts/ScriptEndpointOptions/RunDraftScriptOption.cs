using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The RunDraftScript endpoint.</summary>
[TypeOption(typeof(ScriptEndpoints), "RunDraftScript")]
public class RunDraftScriptOption : ScriptEndpointBase<RunDraftScriptEndpoint>
{
}
