using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceAuth.Endpoints.AgentKeyEndpointOptions;

/// <summary>The endpoints over the agent-key resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "AgentKeyEndpoints")]
[TypeCollection(typeof(AgentKeyEndpointBase), typeof(IEndpointTypeOption), typeof(AgentKeyEndpoints))]
public partial class AgentKeyEndpoints : EndpointTypeCollectionBase<AgentKeyEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
