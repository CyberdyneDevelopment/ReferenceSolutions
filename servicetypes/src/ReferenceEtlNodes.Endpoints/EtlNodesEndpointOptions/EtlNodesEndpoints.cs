using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceEtlNodes.Endpoints.EtlNodesEndpointOptions;

/// <summary>The ETL server's nodes endpoints.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "EtlNodesEndpoints")]
[TypeCollection(typeof(EtlNodesEndpointBase), typeof(IEndpointTypeOption), typeof(EtlNodesEndpoints))]
public partial class EtlNodesEndpoints : EndpointTypeCollectionBase<EtlNodesEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
