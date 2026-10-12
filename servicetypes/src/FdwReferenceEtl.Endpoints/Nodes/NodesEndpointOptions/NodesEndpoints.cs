using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceEtl.Endpoints.Nodes.NodesEndpointOptions;

/// <summary>The endpoints over the nodes surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "NodesEndpoints")]
[TypeCollection(typeof(NodesEndpointBase), typeof(IEndpointTypeOption), typeof(NodesEndpoints))]
public partial class NodesEndpoints : EndpointTypeCollectionBase<NodesEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
