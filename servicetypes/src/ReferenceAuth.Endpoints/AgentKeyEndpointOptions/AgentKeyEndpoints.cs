using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAuth.Endpoints.AgentKeyEndpointOptions;

/// <summary>The endpoints over the agent-key resource.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "AgentKeyEndpoints")]
[TypeCollection(typeof(AgentKeyEndpointBase), typeof(IEndpointTypeOption), typeof(AgentKeyEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "AgentKeyEndpoints")]
public partial class AgentKeyEndpoints : EndpointTypeCollectionBase<AgentKeyEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
