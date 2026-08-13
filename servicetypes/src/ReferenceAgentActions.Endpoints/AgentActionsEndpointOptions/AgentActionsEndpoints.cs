using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The endpoints over the agentactions surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "AgentActionsEndpoints")]
[TypeCollection(typeof(AgentActionsEndpointBase), typeof(IEndpointTypeOption), typeof(AgentActionsEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "AgentActionsEndpoints")]
public partial class AgentActionsEndpoints : EndpointTypeCollectionBase<AgentActionsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
