using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The endpoints over the agentactions surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "AgentActionsEndpoints")]
[TypeCollection(typeof(AgentActionsEndpointBase), typeof(IEndpointTypeOption), typeof(AgentActionsEndpoints))]
public partial class AgentActionsEndpoints : EndpointTypeCollectionBase<AgentActionsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
