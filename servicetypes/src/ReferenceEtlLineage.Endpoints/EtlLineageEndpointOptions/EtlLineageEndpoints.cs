using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEtlLineage.Endpoints.EtlLineageEndpointOptions;

/// <summary>The ETL server's lineage endpoints.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "EtlLineageEndpoints")]
[TypeCollection(typeof(EtlLineageEndpointBase), typeof(IEndpointTypeOption), typeof(EtlLineageEndpoints))]
public partial class EtlLineageEndpoints : EndpointTypeCollectionBase<EtlLineageEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
