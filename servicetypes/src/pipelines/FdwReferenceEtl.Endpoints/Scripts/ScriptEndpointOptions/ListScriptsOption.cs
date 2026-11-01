using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The ListScripts endpoint.</summary>
[TypeOption(typeof(ScriptEndpoints), "ListScripts")]
public class ListScriptsOption : ScriptEndpointBase<ListScriptsEndpoint>
{
}
