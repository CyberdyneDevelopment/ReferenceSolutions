using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceNodes.Endpoints.NodesEndpointOptions;

/// <summary>The endpoints over the nodes surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "NodesEndpoints")]
[TypeCollection(typeof(NodesEndpointBase), typeof(IEndpointTypeOption), typeof(NodesEndpoints))]
public partial class NodesEndpoints : EndpointTypeCollectionBase<NodesEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
