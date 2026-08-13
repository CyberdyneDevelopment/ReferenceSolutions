using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEtlNodes.Endpoints.EtlNodesEndpointOptions;

/// <summary>The ETL server's nodes endpoints.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "EtlNodesEndpoints")]
[TypeCollection(typeof(EtlNodesEndpointBase), typeof(IEndpointTypeOption), typeof(EtlNodesEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "EtlNodesEndpoints")]
public partial class EtlNodesEndpoints : EndpointTypeCollectionBase<EtlNodesEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
