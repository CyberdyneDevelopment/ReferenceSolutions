using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The DeleteScript endpoint.</summary>
[TypeOption(typeof(ScriptEndpoints), "DeleteScript")]
public class DeleteScriptOption : ScriptEndpointBase<DeleteScriptEndpoint>
{
}
