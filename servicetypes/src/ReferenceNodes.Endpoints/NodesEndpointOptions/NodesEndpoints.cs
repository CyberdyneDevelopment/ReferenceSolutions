using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceNodes.Endpoints.NodesEndpointOptions;

/// <summary>The endpoints over the nodes surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "NodesEndpoints")]
[TypeCollection(typeof(NodesEndpointBase), typeof(IEndpointTypeOption), typeof(NodesEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "NodesEndpoints")]
public partial class NodesEndpoints : EndpointTypeCollectionBase<NodesEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
